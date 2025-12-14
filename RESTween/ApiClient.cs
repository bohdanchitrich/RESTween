using Castle.DynamicProxy;
using RESTween.Attributes;
using RESTween.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace RESTween
{
    public class ApiClient
    {
        private readonly IRequestHandler _requestHandler;
        private readonly HttpClient _httpClient;

        public ApiClient(IRequestHandler requestHandler, HttpClient httpClient)
        {
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
            _httpClient = httpClient;
        }

        public async Task<T> CallAsync<T>(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            HttpRequestMessage request = CreateRequest(method, parameterInfos, parameters);
            return await _requestHandler.HandleRequestAsync<T>(request, _httpClient);
        }

        public async Task CallAsync(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            HttpRequestMessage request = CreateRequest(method, parameterInfos, parameters);
            await _requestHandler.HandleRequestAsync(request, _httpClient);
        }


        // ВАЖЛИВО: тепер передаємо methodInfo в Handle*
        public HttpRequestMessage CreateRequest(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            if (method.GetCustomAttribute<GetAttribute>() is GetAttribute getAttr)
                return HandleGet(getAttr.Url, method, parameterInfos, parameters);

            if (method.GetCustomAttribute<PostAttribute>() is PostAttribute postAttr)
                return HandlePost(postAttr.Url, method, parameterInfos, parameters);

            if (method.GetCustomAttribute<PutAttribute>() is PutAttribute putAttr)
                return HandlePut(putAttr.Url, method, parameterInfos, parameters);

            if (method.GetCustomAttribute<DeleteAttribute>() is DeleteAttribute deleteAttr)
                return HandleDelete(deleteAttr.Url, method, parameterInfos, parameters);

            throw new NotImplementedException("Only GET, POST, PUT, and DELETE methods are supported.");
        }

        private HttpRequestMessage HandleGet(string url, MethodInfo methodInfo, ParameterInfo[] parameterInfos, object[] parametersValues)
            => BuildRequest(HttpMethod.Get, url, methodInfo, parameterInfos, parametersValues, complexNoAttrAsBody: false);

        private HttpRequestMessage HandlePost(string url, MethodInfo methodInfo, ParameterInfo[] parameterInfos, object[] parametersValues)
            => BuildRequest(HttpMethod.Post, url, methodInfo, parameterInfos, parametersValues, complexNoAttrAsBody: true);

        private HttpRequestMessage HandlePut(string url, MethodInfo methodInfo, ParameterInfo[] parameterInfos, object[] parametersValues)
            => BuildRequest(HttpMethod.Put, url, methodInfo, parameterInfos, parametersValues, complexNoAttrAsBody: true);

        private HttpRequestMessage HandleDelete(string url, MethodInfo methodInfo, ParameterInfo[] parameterInfos, object[] parametersValues)
            => BuildRequest(HttpMethod.Delete, url, methodInfo, parameterInfos, parametersValues, complexNoAttrAsBody: false);

        private HttpRequestMessage BuildRequest(
            HttpMethod httpMethod,
            string url,
            MethodInfo methodInfo,
            ParameterInfo[] parameterInfos,
            object[] parametersValues,
            bool complexNoAttrAsBody)
        {
            object? body = null;

            var queries = new Dictionary<string, object?>(StringComparer.Ordinal);
            var routes = new Dictionary<string, object?>(StringComparer.Ordinal);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1) headers з атрибутів методу
            CollectMethodHeaders(methodInfo, headers);

            // 2) розбір параметрів
            for (int i = 0; i < parametersValues.Length; i++)
            {
                var value = parametersValues[i];
                var info = parameterInfos[i];
                var paramName = info.Name;
                if (paramName == null) continue;

                // 2.1) Header параметр
                var headerAttr = info.GetCustomAttribute<HeaderAttribute>();
                if (headerAttr != null)
                {
                    var key = headerAttr.Name ?? paramName;
                    if (!string.IsNullOrWhiteSpace(key) && value != null)
                    {
                        // параметр перезаписує метод-значення (пріоритет)
                        headers[key] = FormatHeaderValue(value);
                    }
                    continue;
                }

                // 2.2) Route
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

                // 2.3) Query
                var queryAttr = info.GetCustomAttribute<QueryAttribute>();
                if (queryAttr != null)
                {
                    var queryName = queryAttr.Name ?? paramName;

                    if (ParameterTypeChecker.IsSimpleType(info.ParameterType))
                    {
                        AddQuery(queries, queryName, value, url);
                    }
                    else
                    {
                        ExpandObjectToQuery(queries, value, url);
                    }
                    continue;
                }

                // 2.4) Body (тільки якщо явно позначили)
                var bodyAttr = info.GetCustomAttribute<BodyAttribute>();
                if (bodyAttr != null)
                {
                    if (body != null)
                        throw new Exception($"Request {url} can have only one body parameter");

                    body = value;
                    continue;
                }

                // 2.5) Без атрибутів — дефолтна логіка
                if (!HasAttributes(info))
                {
                    if (ParameterTypeChecker.IsSimpleType(info.ParameterType))
                    {
                        // simple: або route (якщо є {name}), або query
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
                            AddQuery(queries, paramName, value, url);
                        }

                        continue;
                    }

                    // complex без атрибутів:
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

                // якщо сюди дійшли — атрибут(и) є, але не підтримані/не розпізнані
                throw new Exception($"Fail to parse parameter {paramName} in {url}");
            }

            // 3) застосувати routes
            foreach (var rp in routes)
            {
                url = url.Replace($"{{{rp.Key}}}", HttpUtility.UrlEncode(rp.Value?.ToString() ?? string.Empty));
            }

            // 4) зібрати query string
            var query = BuildQueryString(queries);
            if (!string.IsNullOrEmpty(query))
                url = AppendQuery(url, query);

            // 5) створити HttpRequestMessage
            var request = new HttpRequestMessage(httpMethod, url);

            if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json");
            }

            // 6) застосувати headers
            ApplyHeaders(request, headers, url);

            return request;
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

                    // Content-Type краще задавати явно
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
            // мінімально необхідне (можна розширити)
            return headerName.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)
                   || headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                   || headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddQuery(Dictionary<string, object?> queries, string key, object? value, string url)
        {
            if (queries.ContainsKey(key))
                throw new Exception($"{key} duplicated in {url}");

            queries[key] = value;
        }

        private static void ExpandObjectToQuery(Dictionary<string, object?> queries, object? obj, string url)
        {
            if (obj == null) return;

            var actualType = obj.GetType();
            var props = actualType
                .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod?.IsPublic == true || p.GetMethod?.IsAssembly == true)
                .ToArray();

            foreach (var prop in props)
            {
                var key = prop.Name;
                var val = prop.GetValue(obj);
                if (val == null) continue;

                if (queries.ContainsKey(key))
                    throw new Exception($"{key} duplicated in {url}");

                queries[key] = val;
            }
        }

        private static string BuildQueryString(Dictionary<string, object?> queries)
        {
            var parts = new List<string>();

            foreach (var (key, val) in queries)
            {
                if (val == null) continue;

                if (val is string || val is not IEnumerable enumerable)
                {
                    parts.Add($"{key}={HttpUtility.UrlEncode(FormatQueryValue(val))}");
                    continue;
                }

                foreach (var item in enumerable)
                {
                    if (item == null) continue;
                    parts.Add($"{key}={HttpUtility.UrlEncode(FormatQueryValue(item))}");
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
            if (value == null)
                return string.Empty;

            switch (value)
            {
                case bool b:
                    return b ? "true" : "false";

                case DateTime dt:
                    return dt.Kind == DateTimeKind.Unspecified
                        ? dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                        : dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);

#if NET6_0_OR_GREATER
                case DateOnly d:
                    return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                case TimeOnly t:
                    return t.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
#endif

                case Enum e:
                    return GetEnumValue(e);

                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);

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
    }




}


