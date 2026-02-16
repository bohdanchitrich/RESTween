using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RESTween.Handlers
{
    public sealed class DefaultRequestHandler : IRequestHandler
    {
        public async Task<T> HandleRequestAsync<T>(RequestContext context, HttpClient httpClient)
        {
            var response = await httpClient.SendAsync(context.Request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>() ?? default; ;
        }

        public async Task HandleRequestAsync(RequestContext context, HttpClient httpClient)
        {
            var response = await httpClient.SendAsync(context.Request);
            response.EnsureSuccessStatusCode();
        }
    }
}
