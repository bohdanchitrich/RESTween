using RESTween.Building;
using RESTween.Handlers;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace RESTween
{
    public class ApiClient
    {
        private readonly IRequestHandler _requestHandler;
        private readonly HttpClient _httpClient;
        private readonly IRestweenRequestBuilder _requestBuilder;

        internal ApiClient(IRequestHandler requestHandler, HttpClient httpClient)
            : this(requestHandler, httpClient, DefaultRestweenRequestBuilder.CreateDefault())
        {
        }

        internal ApiClient(
            IRequestHandler requestHandler,
            HttpClient httpClient,
            IRestweenRequestBuilder requestBuilder)
        {
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        }

        public async Task<T> CallAsync<T>(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var request = CreateRequest(method, parameterInfos, parameters);
            var attributes = method.GetCustomAttributes<Attribute>(true).ToList();
            var context = new RequestContext(request, attributes);

            return await _requestHandler.HandleRequestAsync<T>(context, _httpClient);
        }

        public async Task CallAsync(MethodInfo method, ParameterInfo[] parameterInfos, object[] parameters)
        {
            var request = CreateRequest(method, parameterInfos, parameters);
            var attributes = method.GetCustomAttributes<Attribute>(true).ToList();
            var context = new RequestContext(request, attributes);

            await _requestHandler.HandleRequestAsync(context, _httpClient);
        }

        public HttpRequestMessage CreateRequest(MethodInfo method, ParameterInfo[] parameterInfos, object?[] parameters)
        {
            return _requestBuilder.Build(method, parameterInfos, parameters);
        }
    }
}
