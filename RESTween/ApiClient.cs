using Castle.DynamicProxy;
using RESTween.Attributes;
using RESTween.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
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
            HttpRequestMessage request;

            if (method.GetCustomAttribute<GetAttribute>() is GetAttribute getAttr)
            {
                string url = getAttr.Url;
                if (parameters.Length > 0)
                {
                    var query = string.Join("&", parameters.Select((param, index) => $"param{index}={param}"));
                    url += "?" + query;
                }
                request = new HttpRequestMessage(HttpMethod.Get, url);
            }
            else if (method.GetCustomAttribute<PostAttribute>() is PostAttribute postAttr)
            {
                string url = postAttr.Url;
                var content = new StringContent(JsonSerializer.Serialize(parameters[0]), Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
            }
            else
            {
                throw new NotImplementedException("Only GET and POST methods are supported.");
            }

            return request;
        }
    }

}
