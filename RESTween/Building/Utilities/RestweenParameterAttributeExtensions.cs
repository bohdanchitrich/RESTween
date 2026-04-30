using RESTween.Attributes;
using System.Reflection;

namespace RESTween.Building
{
    internal static class RestweenParameterAttributeExtensions
    {
        internal static bool HasRestweenBindingAttribute(this ParameterInfo parameter)
        {
            return parameter.IsDefined(typeof(QueryAttribute), inherit: true)
                   || parameter.IsDefined(typeof(RouteAttribute), inherit: true)
                   || parameter.IsDefined(typeof(BodyAttribute), inherit: true)
                   || parameter.IsDefined(typeof(HeaderAttribute), inherit: true);
        }
    }
}
