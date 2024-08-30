using Castle.DynamicProxy;


namespace RESTween
{
    public class ApiServiceInterceptor<T> : IAsyncInterceptor
    {
        private readonly ApiClient _apiClient;

        public ApiServiceInterceptor(ApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public void InterceptSynchronous(IInvocation invocation)
        {
            throw new NotSupportedException("Synchronous methods are not supported.");
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InterceptAsync(invocation);
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InterceptAsync<TResult>(invocation);
        }

        private async Task InterceptAsync(IInvocation invocation)
        {
            await InterceptVoidAsync(invocation);
        }

        private async Task InterceptVoidAsync(IInvocation invocation)
        {
            var methodInfo = invocation.Method;
            var parameters = methodInfo.GetParameters();
            var arguments = invocation.Arguments;
            await _apiClient.CallAsync(invocation.Method, parameters, arguments);
        }

        private async Task<TResult> InterceptAsync<TResult>(IInvocation invocation)
        {
            var methodInfo = invocation.Method;
            var parameters = methodInfo.GetParameters();
            var arguments = invocation.Arguments;
            return await _apiClient.CallAsync<TResult>(invocation.Method, parameters, arguments);
        }
    }
}
