using Castle.DynamicProxy;
using RESTween.Attributes;
using RESTween.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task<T> CallAsync<T>(MethodInfo method, object[] parameters)
        {
            HttpRequestMessage request = CreateRequest(method, parameters);
            return await _requestHandler.HandleRequestAsync<T>(request, _httpClient);
        }

        public async Task CallAsync(MethodInfo method, object[] parameters)
        {
            HttpRequestMessage request = CreateRequest(method, parameters);
            await _requestHandler.HandleRequestAsync(request, _httpClient);
        }

        private HttpRequestMessage CreateRequest(MethodInfo method, object[] parameters)
        {
            HttpRequestMessage? request = null;

            if (method.GetCustomAttribute<GetAttribute>() is GetAttribute getAttr)
            {
                request = HandleGet(getAttr.Url, parameters);
            }
            else if (method.GetCustomAttribute<PostAttribute>() is PostAttribute postAttr)
            {
                request = HandlePost(postAttr.Url, parameters);
            }
            else if (method.GetCustomAttribute<PutAttribute>() is PutAttribute putAttr)
            {
                request = HandlePut(putAttr.Url, parameters);
            }
            else if (method.GetCustomAttribute<DeleteAttribute>() is DeleteAttribute deleteAttr)
            {
                request = HandleDelete(deleteAttr.Url, parameters);
            }
            else
            {
                throw new NotImplementedException("Only GET, POST, PUT, and DELETE methods are supported.");
            }

            return request;
        }

        private HttpRequestMessage HandleGet(string url, object[] parameters)
        {
            var queryParams = new Dictionary<string, string>();

            foreach (var parameter in parameters)
            {
                var paramType = parameter.GetType();
                var parameterInfo = paramType.GetProperties();
                foreach (var prop in parameterInfo)
                {
                    var queryAttr = prop.GetCustomAttribute<QueryAttribute>();
                    var value = prop.GetValue(parameter)?.ToString();
                    if (value == null) continue;
                    if (queryAttr != null)
                    {
                        queryParams[queryAttr.Name] = value;
                    }
                    else
                    {
                        queryParams[prop.Name] = value;
                    }
                }
            }

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
            var requestUrl = string.IsNullOrWhiteSpace(queryString) ? url : $"{url}?{queryString}";

            return new HttpRequestMessage(HttpMethod.Get, requestUrl);
        }

        private HttpRequestMessage HandlePost(string url, object[] parameters)
        {
            object bodyContent = null;
            var queryParams = new Dictionary<string, string>();

            foreach (var parameter in parameters)
            {
                var paramType = parameter.GetType();
                var paramAttr = paramType.GetCustomAttribute<BodyAttribute>();
                var queryAttr = paramType.GetCustomAttribute<QueryAttribute>();

                if (paramAttr != null)
                {
                    if (bodyContent != null)
                    {
                        throw new ArgumentException("Multiple parameters cannot be used as body content. Please remove additional parameters or mark them as [Query].");
                    }
                    bodyContent = parameter;
                }
                else if (queryAttr != null)
                {
                    queryParams[queryAttr.Name] = parameter.ToString();
                }
                else if (parameters.Length == 1)
                {
                    bodyContent = parameter;
                }
                else
                {
                    throw new ArgumentException("Multiple parameters without attributes detected. Only one parameter can be used as body content, or others must be marked with [Query].");
                }
            }

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
            var requestUrl = string.IsNullOrWhiteSpace(queryString) ? url : $"{url}?{queryString}";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = bodyContent != null ? new StringContent(JsonSerializer.Serialize(bodyContent), Encoding.UTF8, "application/json") : null
            };

            return httpRequestMessage;
        }

        private HttpRequestMessage HandlePut(string url, object[] parameters)
        {
            var result = HandlePost(url, parameters);
            result.Method = HttpMethod.Put;
            return result;
        }

        private HttpRequestMessage HandleDelete(string url, object[] parameters)
        {
            var queryParams = new Dictionary<string, string>();

            foreach (var parameter in parameters)
            {
                var paramType = parameter.GetType();
                var parameterInfo = paramType.GetProperties();
                foreach (var prop in parameterInfo)
                {
                    var queryAttr = prop.GetCustomAttribute<QueryAttribute>();
                    var value = prop.GetValue(parameter)?.ToString();
                    if (value == null) continue;
                    if (queryAttr != null)
                    {
                        queryParams[queryAttr.Name] = value;
                    }
                    else
                    {
                        queryParams[prop.Name] = value;
                    }
                }
            }

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
            var requestUrl = string.IsNullOrWhiteSpace(queryString) ? url : $"{url}?{queryString}";

            return new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        }
    }

}

  
