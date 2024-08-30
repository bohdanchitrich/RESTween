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

        public HttpRequestMessage CreateRequest(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            HttpRequestMessage? request = null;

            if (method.GetCustomAttribute<GetAttribute>() is GetAttribute getAttr)
            {
                request = HandleGet(getAttr.Url, parameterInfos, parameters);
            }
            else if (method.GetCustomAttribute<PostAttribute>() is PostAttribute postAttr)
            {
                request = HandlePost(postAttr.Url, parameterInfos, parameters);
            }
            else if (method.GetCustomAttribute<PutAttribute>() is PutAttribute putAttr)
            {
                request = HandlePut(putAttr.Url, parameterInfos, parameters);
            }
            else if (method.GetCustomAttribute<DeleteAttribute>() is DeleteAttribute deleteAttr)
            {
                request = HandleDelete(deleteAttr.Url, parameterInfos, parameters);
            }
            else
            {
                throw new NotImplementedException("Only GET, POST, PUT, and DELETE methods are supported.");
            }

            return request;
        }
        private HttpRequestMessage HandleGet(string url, ParameterInfo[] parameterInfos, object[] parameters)
        {
            if (parameters.Length > 0)
            {
                var query = string.Join("&", parameters.Select((param, index) =>
                {
                    var queryAttribute = parameterInfos[index].GetCustomAttribute<QueryAttribute>();
                    var paramName = queryAttribute?.Name ?? parameterInfos[index].Name;
                    return $"{paramName}={param}";
                }));

                url += "?" + query;
            }

            return new HttpRequestMessage(HttpMethod.Get, url);
        }

        private HttpRequestMessage HandlePost(string url, ParameterInfo[] parameterInfos, object[] parameters)
        {
            object bodyContent = null;
            var queryParams = new Dictionary<string, string>();

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterInfo = parameterInfos[i];

                if (parameterInfo.GetCustomAttribute<BodyAttribute>() != null)
                {
                    if (bodyContent != null)
                    {
                        throw new ArgumentException("Multiple parameters cannot be used as body content. Please remove additional parameters or mark them as [Query].");
                    }
                    bodyContent = parameter;
                }
                else if (parameterInfo.GetCustomAttribute<QueryAttribute>() != null)
                {
                    queryParams[parameterInfo.Name] = parameter.ToString();
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

        private HttpRequestMessage HandlePut(string url, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var result = HandlePost(url, parameterInfos, parameters);
            result.Method = HttpMethod.Put;
            return result;
        }

        private HttpRequestMessage HandleDelete(string url, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var result = HandleGet(url, parameterInfos, parameters);
            result.Method = HttpMethod.Delete;
            return result;
        }
    }

}


