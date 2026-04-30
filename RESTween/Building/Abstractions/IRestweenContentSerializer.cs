using System.Net.Http;

namespace RESTween.Building
{
    public interface IRestweenContentSerializer
    {
        HttpContent SerializeJsonContent(object value);

        HttpContent SerializeMultipartJsonContent(object value);
    }
}
