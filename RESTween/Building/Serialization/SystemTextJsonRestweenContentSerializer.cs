using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace RESTween.Building
{
    public sealed class SystemTextJsonRestweenContentSerializer : IRestweenContentSerializer
    {
        private readonly JsonSerializerOptions? _options;

        public SystemTextJsonRestweenContentSerializer()
        {
        }

        public SystemTextJsonRestweenContentSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public HttpContent SerializeJsonContent(object value)
        {
            return new StringContent(JsonSerializer.Serialize(value, _options), Encoding.UTF8, "application/json");
        }

        public HttpContent SerializeMultipartJsonContent(object value)
        {
            return new StringContent(JsonSerializer.Serialize(value, _options), Encoding.UTF8, "application/json");
        }
    }
}
