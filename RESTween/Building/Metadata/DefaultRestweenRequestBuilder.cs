using RESTween.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Web;

namespace RESTween.Building
{
    public sealed class DefaultRestweenRequestBuilder : IRestweenRequestBuilder
    {
        private readonly HttpMethodMetadataReader _metadataReader;
        private readonly IReadOnlyList<IRestweenParameterBinder> _binders;
        private readonly IRestweenContentSerializer _serializer;
        private readonly IRestweenValueFormatter _formatter;

        public DefaultRestweenRequestBuilder(
            HttpMethodMetadataReader metadataReader,
            IEnumerable<IRestweenParameterBinder> binders,
            IRestweenContentSerializer serializer,
            IRestweenValueFormatter formatter)
        {
            _metadataReader = metadataReader ?? throw new ArgumentNullException(nameof(metadataReader));
            _binders = binders?.ToArray() ?? throw new ArgumentNullException(nameof(binders));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        }

        public HttpRequestMessage Build(MethodInfo method, ParameterInfo[] parameterInfos, object?[] parameters)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (parameterInfos == null)
                throw new ArgumentNullException(nameof(parameterInfos));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (parameterInfos.Length != parameters.Length)
                throw new RestweenRequestBuildException("Parameter metadata count does not match argument count.");

            var metadata = _metadataReader.Read(method);
            var state = new RestweenRequestState(metadata);

            CollectMethodHeaders(method, state);

            for (var i = 0; i < parameters.Length; i++)
            {
                var context = new RestweenParameterContext(parameterInfos[i], parameters[i], metadata, state, i);
                var handled = false;

                foreach (var binder in _binders)
                {
                    if (!binder.TryBind(context))
                        continue;

                    handled = true;
                    break;
                }

                if (!handled)
                {
                    var parameterName = parameterInfos[i].Name ?? i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    throw new RestweenRequestBuildException($"Fail to parse parameter {parameterName} in {state.Url}");
                }
            }

            ApplyRoutes(state);
            var queryString = BuildQueryString(state.Queries);
            if (!string.IsNullOrEmpty(queryString))
                state.Url = AppendQuery(state.Url, queryString);

            var request = new HttpRequestMessage(metadata.HttpMethod, state.Url);

            if ((metadata.HttpMethod == HttpMethod.Get || metadata.HttpMethod == HttpMethod.Delete) && state.HasBody)
                throw new RestweenRequestBuildException($"Request {state.Url} with method {metadata.HttpMethod} cannot contain body.");

            if (state.MultipartContent != null)
            {
                request.Content = state.MultipartContent;
                ApplyHeaders(request, state.Headers, state.Url);
                return request;
            }

            if (state.HasBody && state.Body != null)
                request.Content = _serializer.SerializeJsonContent(state.Body);

            ApplyHeaders(request, state.Headers, state.Url);
            return request;
        }

        public static DefaultRestweenRequestBuilder CreateDefault()
        {
            var serializer = new SystemTextJsonRestweenContentSerializer();
            var formatter = new DefaultRestweenValueFormatter();
            return new DefaultRestweenRequestBuilder(
                new HttpMethodMetadataReader(),
                new IRestweenParameterBinder[]
                {
                    new MultipartParameterBinder(serializer),
                    new HeaderParameterBinder(formatter),
                    new RouteParameterBinder(),
                    new QueryParameterBinder(),
                    new BodyParameterBinder()
                },
                serializer,
                formatter);
        }

        private static void CollectMethodHeaders(MethodInfo methodInfo, RestweenRequestState state)
        {
            var attrs = methodInfo.GetCustomAttributes<HeadersAttribute>(inherit: true);
            foreach (var attr in attrs)
            {
                foreach (var line in attr.HeaderLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length != 2)
                        throw new RestweenRequestBuildException($"Invalid header format in [Headers]: {line}");

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (string.IsNullOrWhiteSpace(key))
                        throw new RestweenRequestBuildException($"Invalid header key in [Headers]: {line}");

                    if (state.Headers.ContainsKey(key))
                        throw new RestweenRequestBuildException($"Duplicate header key {key} in method attributes");

                    state.AddHeader(key, value);
                }
            }
        }

        private void ApplyRoutes(RestweenRequestState state)
        {
            foreach (var route in state.RouteValues)
            {
                state.Url = state.Url.Replace(
                    "{" + route.Key + "}",
                    HttpUtility.UrlEncode(_formatter.FormatRouteValue(route.Value)));
            }
        }

        private string BuildQueryString(IReadOnlyList<RestweenQueryValue> queries)
        {
            var parts = new List<string>();

            foreach (var query in queries)
            {
                if (query.Value == null)
                    continue;

                if (query.Value is string || query.Value is not IEnumerable enumerable)
                {
                    parts.Add($"{query.Name}={HttpUtility.UrlEncode(_formatter.FormatQueryValue(query.Value))}");
                    continue;
                }

                foreach (var item in enumerable)
                {
                    if (item == null)
                        continue;

                    var name = query.Name;
                    if (query.CollectionFormat == CollectionFormat.Multi
                        && !name.EndsWith("[]", StringComparison.Ordinal))
                    {
                        name += "[]";
                    }

                    parts.Add($"{name}={HttpUtility.UrlEncode(_formatter.FormatQueryValue(item))}");
                }
            }

            return string.Join("&", parts);
        }

        private static string AppendQuery(string url, string query)
        {
            return url.Contains("?") ? $"{url}&{query}" : $"{url}?{query}";
        }

        private static void ApplyHeaders(HttpRequestMessage request, IDictionary<string, string> headers, string url)
        {
            foreach (var header in headers)
            {
                if (IsContentHeader(header.Key))
                {
                    if (request.Content == null)
                        throw new RestweenRequestBuildException($"Content header '{header.Key}' specified for {url}, but request has no body/content.");

                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header.Value);
                        continue;
                    }

                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        private static bool IsContentHeader(string headerName)
        {
            return headerName.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)
                   || headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                   || headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase);
        }
    }
}
