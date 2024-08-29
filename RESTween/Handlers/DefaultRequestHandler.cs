using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RESTween.Handlers
{
    public sealed class DefaultRequestHandler : IRequestHandler
    {
        public async Task<T> HandleRequestAsync<T>(HttpRequestMessage request, HttpClient httpClient)
        {
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(content);
            }
            return JsonSerializer.Deserialize<T>(content) ?? default;
        }

        public async Task HandleRequestAsync(HttpRequestMessage request, HttpClient httpClient)
        {
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(content);
            }
        }
    }
}
