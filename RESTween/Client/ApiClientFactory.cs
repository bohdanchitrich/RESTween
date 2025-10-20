using Castle.DynamicProxy;
using RESTween.Client.Handlers;
using System.Net.Http;

namespace RESTween.Client
{
    public static class ApiClientFactory
    {
        private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

        public static T CreateClient<T>(HttpClient httpClient, IRequestHandler requestHandler) where T : class
        {
            var apiClient = new ApiClient(requestHandler, httpClient);
            var interceptor = new ApiServiceInterceptor<T>(apiClient);

            return _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(new AsyncDeterminationInterceptor(interceptor));
        }
    }

}
