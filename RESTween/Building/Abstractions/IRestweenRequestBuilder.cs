using System.Net.Http;
using System.Reflection;

namespace RESTween.Building
{
    public interface IRestweenRequestBuilder
    {
        HttpRequestMessage Build(MethodInfo method, ParameterInfo[] parameterInfos, object?[] parameters);
    }
}
