using RESTween.Attributes;
using System.Reflection;

namespace RESTween.Building
{
    public sealed class RouteParameterBinder : IRestweenParameterBinder
    {
        public bool TryBind(RestweenParameterContext context)
        {
            var parameterName = context.Parameter.Name;
            if (parameterName == null)
                return true;

            var routeAttr = context.Parameter.GetCustomAttribute<RouteAttribute>(true);
            if (routeAttr != null)
            {
                context.State.AddRoute(routeAttr.Name ?? parameterName, context.Value);
                return true;
            }

            if (context.Parameter.HasRestweenBindingAttribute())
                return false;

            if (RestweenTypeUtilities.IsSimpleType(context.Parameter.ParameterType)
                && context.Metadata.UrlTemplate.Contains("{" + parameterName + "}"))
            {
                context.State.AddRoute(parameterName, context.Value);
                return true;
            }

            return false;
        }
    }
}
