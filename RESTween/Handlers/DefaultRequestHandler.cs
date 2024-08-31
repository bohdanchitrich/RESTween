using System.Net.Http.Json;

namespace RESTween.Handlers
{
    public sealed class DefaultRequestHandler : IRequestHandler
    {
        public async Task<T> HandleRequestAsync<T>(HttpRequestMessage request, HttpClient httpClient)
        {
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>() ?? default;
        }

        public async Task HandleRequestAsync(HttpRequestMessage request, HttpClient httpClient)
        {
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
