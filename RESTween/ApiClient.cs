using Castle.DynamicProxy;
using RESTween.Attributes;
using RESTween.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
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
            var usedParameters = new HashSet<string>();

            url = ReplaceRouteParameters(url, parameterInfos, parameters, usedParameters);

            var query = string.Join("&", parameters.Select((param, index) =>
            {
                var paramName = parameterInfos[index].Name;

                if (usedParameters.Contains(paramName))
                    return null; 

                var queryAttribute = parameterInfos[index].GetCustomAttribute<QueryAttribute>();
                paramName = queryAttribute?.Name ?? paramName;
                return $"{paramName}={param}";
            }).Where(q => q != null));

            if (!string.IsNullOrWhiteSpace(query))
            {
                url += "?" + query;
            }

            return new HttpRequestMessage(HttpMethod.Get, url);
        }

        private HttpRequestMessage HandlePost(string url, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var usedParameters = new HashSet<string>();

            url = ReplaceRouteParameters(url, parameterInfos, parameters, usedParameters);

            object? bodyContent = null;
            var queryParams = new Dictionary<string, string>();

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var paramName = parameterInfos[i].Name;

                if (usedParameters.Contains(paramName))
                    continue;

                var bodyAttribute = parameterInfos[i].GetCustomAttribute<BodyAttribute>();
                var queryAttribute = parameterInfos[i].GetCustomAttribute<QueryAttribute>();

                if (bodyAttribute != null)
                {
                    if (bodyContent != null)
                    {
                        throw new ArgumentException("Multiple parameters cannot be used as body content. Please remove additional parameters or mark them as [Query].");
                    }
                    bodyContent = parameter;
                }
                else
                {
                    paramName = queryAttribute?.Name ?? paramName;
                    queryParams[paramName] = parameter.ToString();
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

        private string ReplaceRouteParameters(string url, ParameterInfo[] parameterInfos, object[] parameters, HashSet<string> usedParameters)
        {
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                var paramInfo = parameterInfos[i];
                var paramName = paramInfo.Name;

                if (url.Contains($"{{{paramName}}}"))
                {
                    url = url.Replace($"{{{paramName}}}", parameters[i].ToString());
                    usedParameters.Add(paramName);  
                }
            }

            return url;
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


