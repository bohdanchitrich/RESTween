using RESTween.Attributes;
using RESTween.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace RESTween
{
    public class ApiClient
    {
        private readonly IRequestHandler _requestHandler;
        private readonly HttpClient _httpClient;

        internal ApiClient(IRequestHandler requestHandler, HttpClient httpClient)
        {
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }


        public async Task<T> CallAsync<T>(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var request = CreateRequest(method, parameterInfos, parameters);
            var attributes = method
                            .GetCustomAttributes<Attribute>(true)
                            .ToList();

            var context = new RequestContext(request, attributes);



            return await _requestHandler.HandleRequestAsync<T>(context, _httpClient);
        }

        public async Task CallAsync(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var request = CreateRequest(method, parameterInfos, parameters);
            var attributes = method
                     .GetCustomAttributes<Attribute>(true)
                     .ToList();

            var context = new RequestContext(request, attributes);
            await _requestHandler.HandleRequestAsync(context, _httpClient);
        }
        #region CreateRequest
        public HttpRequestMessage CreateRequest(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            if (method.GetCustomAttribute<GetAttribute>() is GetAttribute getAttr)
                return BuildRequest(HttpMethod.Get, getAttr.Url, method, parameterInfos, parameters, complexNoAttrAsBody: false);

            if (method.GetCustomAttribute<PostAttribute>() is PostAttribute postAttr)
                return BuildRequest(HttpMethod.Post, postAttr.Url, method, parameterInfos, parameters, complexNoAttrAsBody: true);

            if (method.GetCustomAttribute<PutAttribute>() is PutAttribute putAttr)
                return BuildRequest(HttpMethod.Put, putAttr.Url, method, parameterInfos, parameters, complexNoAttrAsBody: true);

            if (method.GetCustomAttribute<DeleteAttribute>() is DeleteAttribute deleteAttr)
                return BuildRequest(HttpMethod.Delete, deleteAttr.Url, method, parameterInfos, parameters, complexNoAttrAsBody: false);

            throw new NotImplementedException("Only GET, POST, PUT, and DELETE methods are supported.");
        }

        private HttpRequestMessage BuildRequest(
            HttpMethod httpMethod,
            string url,
            MethodInfo methodInfo,
            ParameterInfo[] parameterInfos,
            object[] parametersValues,
            bool complexNoAttrAsBody)
        {
            object? body = null;
            var isMultipart = methodInfo.GetCustomAttribute<MultipartAttribute>() != null;

            MultipartFormDataContent? multipart = null;

            if (isMultipart)
            {
                multipart = new MultipartFormDataContent();
            }
            var queries = new List<QueryItem>();
            var routes = new Dictionary<string, object?>(StringComparer.Ordinal);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            CollectMethodHeaders(methodInfo, headers);

            for (int i = 0; i < parametersValues.Length; i++)
            {
                var value = parametersValues[i];
                var info = parameterInfos[i];
                var paramName = info.Name;
                if (paramName == null) continue;


                if (multipart != null)
                {
                    var name = paramName;

                    if (value == null)
                        continue;

                    // Stream
                    if (value is Stream stream)
                    {
                        var content = new StreamContent(stream);
                        multipart.Add(content, name, "file");
                        continue;
                    }

                    // byte[]
                    if (value is byte[] bytes)
                    {
                        var content = new ByteArrayContent(bytes);
                        multipart.Add(content, name, "file");
                        continue;
                    }

                    // FileInfo
                    if (value is FileInfo fileInfo)
                    {
                        var streamFileInfo = fileInfo.OpenRead();
                        var content = new StreamContent(streamFileInfo);

                        multipart.Add(content, name, fileInfo.Name);
                        continue;
                    }

                    // прості типи
                    if (ParameterTypeChecker.IsSimpleType(info.ParameterType))
                    {
                        multipart.Add(new StringContent(value.ToString()!), name);
                        continue;
                    }

                    // складний DTO → JSON
                    var json = JsonSerializer.Serialize(value);

                    multipart.Add(
                        new StringContent(json, Encoding.UTF8, "application/json"),
                        name
                    );

                    continue;
                }



                // Header
                var headerAttr = info.GetCustomAttribute<HeaderAttribute>();
                if (headerAttr != null)
                {
                    var key = headerAttr.Name ?? paramName;
                    if (!string.IsNullOrWhiteSpace(key) && value != null)
                        headers[key] = FormatHeaderValue(value);

                    continue;
                }

                // Route
                var routeAttr = info.GetCustomAttribute<RouteAttribute>();
                if (routeAttr != null)
                {
                    var routeName = routeAttr.Name ?? paramName;

                    if (!url.Contains($"{{{routeName}}}"))
                        throw new Exception($"Route {url} not contain {routeName} parameter");

                    if (value == null)
                        throw new Exception($"Route parameter {routeName} in {url} cannot be null");

                    if (routes.ContainsKey(routeName))
                        throw new Exception($"{routeName} duplicated in {url}");

                    routes[routeName] = value;
                    continue;
                }

                // Query
                var queryAttr = info.GetCustomAttribute<QueryAttribute>();
                if (queryAttr != null)
                {
                    var queryName = queryAttr.Name ?? paramName;
                    // scalar OR collection -> AddQuery
                    if (ParameterTypeChecker.IsSimpleType(info.ParameterType) || IsQueryCollection(info.ParameterType))
                    {
                        AddQuery(
                            queries,
                            queryName,
                            value,
                            url,
                            queryAttr.CollectionFormat
                        );
                    }
                    else
                    {
                        // complex object -> expand props into query
                        ExpandObjectToQuery(queries, value, url);
                    }

                    continue;
                }

                // Body
                var bodyAttr = info.GetCustomAttribute<BodyAttribute>();
                if (bodyAttr != null)
                {
                    if (body != null)
                        throw new Exception($"Request {url} can have only one body parameter");

                    body = value;
                    continue;
                }

                // No attributes: default routing
                if (!HasAttributes(info))
                {
                    if (ParameterTypeChecker.IsSimpleType(info.ParameterType))
                    {
                        // simple: route if {name} exists, otherwise query
                        if (url.Contains($"{{{paramName}}}"))
                        {
                            if (value == null)
                                throw new Exception($"Route parameter {paramName} in {url} cannot be null");

                            if (routes.ContainsKey(paramName))
                                throw new Exception($"{paramName} duplicated in {url}");

                            routes[paramName] = value;
                        }
                        else
                        {
                            AddQuery(
                                queries,
                                paramName,
                                value,
                                url,
                                CollectionFormat.Default
                            );
                        }

                        continue;
                    }

                    // collection without attributes: treat as query (default)
                    if (IsQueryCollection(info.ParameterType))
                    {
                        AddQuery(
                            queries,
                            paramName,
                            value,
                            url,
                            CollectionFormat.Default
                        );
                        continue;
                    }

                    // complex without attributes
                    if (complexNoAttrAsBody)
                    {
                        if (body != null)
                            throw new Exception($"Request {url} can have only one body parameter");

                        body = value;
                    }
                    else
                    {
                        ExpandObjectToQuery(queries, value, url);
                    }

                    continue;
                }

                throw new Exception($"Fail to parse parameter {paramName} in {url}");
            }

            // Apply routes
            foreach (var rp in routes)
            {
                url = url.Replace(
                    $"{{{rp.Key}}}",
                    HttpUtility.UrlEncode(rp.Value?.ToString() ?? string.Empty)
                );
            }

            // Build query string
            var queryString = BuildQueryString(queries);
            if (!string.IsNullOrEmpty(queryString))
                url = AppendQuery(url, queryString);

            // Create request
            var request = new HttpRequestMessage(httpMethod, url);

            // For strictness: forbid body on GET/DELETE unless you explicitly want to allow it
            if ((httpMethod == HttpMethod.Get || httpMethod == HttpMethod.Delete) && body != null)
                throw new Exception($"Request {url} with method {httpMethod} cannot contain body.");
            if (multipart != null)
            {
                request.Content = multipart;
                ApplyHeaders(request, headers, url);
                return request;
            }
            if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                );
            }

            ApplyHeaders(request, headers, url);
            return request;
        }

        private static bool IsQueryCollection(Type type)
        {
            if (type == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        private static void CollectMethodHeaders(MethodInfo methodInfo, Dictionary<string, string> headers)
        {
            var attrs = methodInfo.GetCustomAttributes<HeadersAttribute>(inherit: true);
            foreach (var attr in attrs)
            {
                foreach (var line in attr.HeaderLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(':', 2);
                    if (parts.Length != 2)
                        throw new Exception($"Invalid header format in [Headers]: {line}");

                    var key = parts[0].Trim();
                    var val = parts[1].Trim();

                    if (string.IsNullOrWhiteSpace(key))
                        throw new Exception($"Invalid header key in [Headers]: {line}");

                    if (headers.ContainsKey(key))
                        throw new Exception($"Duplicate header key {key} in method attributes");

                    headers[key] = val;
                }
            }
        }

        private static void ApplyHeaders(HttpRequestMessage request, Dictionary<string, string> headers, string url)
        {
            foreach (var (key, value) in headers)
            {
                if (IsContentHeader(key))
                {
                    if (request.Content == null)
                        throw new Exception($"Content header '{key}' specified for {url}, but request has no body/content.");

                    if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
                        continue;
                    }

                    request.Content.Headers.TryAddWithoutValidation(key, value);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(key, value);
                }
            }
        }

        private static bool IsContentHeader(string headerName)
        {
            return headerName.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)
                   || headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                   || headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddQuery(
            List<QueryItem> queries,
            string key,
            object? value,
            string url,
            CollectionFormat collectionFormat)
        {
            if (value == null) return;

            if (collectionFormat == CollectionFormat.Default
                && queries.Any(q => q.Name.Equals(key, StringComparison.Ordinal)))
            {
                throw new Exception($"{key} duplicated in {url}");
            }

            queries.Add(new QueryItem(key, value, collectionFormat));
        }

        private static void ExpandObjectToQuery(List<QueryItem> queries, object? obj, string url)
        {
            if (obj == null) return;

            var props = obj.GetType()
                .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod?.IsPublic == true || p.GetMethod?.IsAssembly == true)
                .ToArray();

            foreach (var prop in props)
            {
                var val = prop.GetValue(obj);
                if (val == null) continue;

                var json = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;

                var key =  json ?? prop.Name;

                if (queries.Any(q => q.Name.Equals(key, StringComparison.Ordinal)))
                    throw new Exception($"{key} duplicated in {url}");

                queries.Add(new QueryItem(key, val, CollectionFormat.Default));
            }
        }

        private static string BuildQueryString(List<QueryItem> queries)
        {
            var parts = new List<string>();

            foreach (var q in queries)
            {
                if (q.Value == null) continue;

                if (q.Value is string || q.Value is not IEnumerable enumerable)
                {
                    parts.Add($"{q.Name}={HttpUtility.UrlEncode(FormatQueryValue(q.Value))}");
                    continue;
                }

                foreach (var item in enumerable)
                {
                    if (item == null) continue;

                    var name = q.Name;

                    if (q.CollectionFormat == CollectionFormat.Multi)
                    {
                        if (!name.EndsWith("[]", StringComparison.Ordinal))
                            name += "[]";
                    }

                    parts.Add($"{name}={HttpUtility.UrlEncode(FormatQueryValue(item))}");
                }
            }

            return string.Join("&", parts);
        }

        private static string AppendQuery(string url, string query)
        {
            return url.Contains('?') ? $"{url}&{query}" : $"{url}?{query}";
        }

        private static string FormatHeaderValue(object value)
        {
            return value switch
            {
                DateTime dt => dt.ToString("r"),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static bool HasAttributes(ParameterInfo parameterInfo)
            => parameterInfo.IsDefined(typeof(QueryAttribute), inherit: true)
               || parameterInfo.IsDefined(typeof(RouteAttribute), inherit: true)
               || parameterInfo.IsDefined(typeof(BodyAttribute), inherit: true)
               || parameterInfo.IsDefined(typeof(HeaderAttribute), inherit: true);

        private static string FormatQueryValue(object value)
        {
            switch (value)
            {
                case bool b:
                    return b ? "true" : "false";

                case DateTime dt:
                    return dt.Kind == DateTimeKind.Unspecified
                        ? dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                        : dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);

                case Enum e:
                    return GetEnumValue(e);

                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        private static string GetEnumValue(Enum value)
        {
            var member = value
                .GetType()
                .GetMember(value.ToString())
                .FirstOrDefault();

            var attr = member?.GetCustomAttribute<EnumMemberAttribute>();
            return attr?.Value ?? value.ToString();
        }

        private sealed record QueryItem(string Name, object Value, CollectionFormat CollectionFormat);
        #endregion
       
    }
}
