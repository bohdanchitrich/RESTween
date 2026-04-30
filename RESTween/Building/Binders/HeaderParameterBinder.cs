using RESTween.Attributes;
using System.Reflection;

namespace RESTween.Building
{
    public sealed class HeaderParameterBinder : IRestweenParameterBinder
    {
        private readonly IRestweenValueFormatter _formatter;

        public HeaderParameterBinder(IRestweenValueFormatter formatter)
        {
            _formatter = formatter;
        }

        public bool TryBind(RestweenParameterContext context)
        {
            var headerAttr = context.Parameter.GetCustomAttribute<HeaderAttribute>(true);
            if (headerAttr == null)
                return false;

            var parameterName = context.Parameter.Name;
            var key = headerAttr.Name ?? parameterName;

            if (!string.IsNullOrWhiteSpace(key) && context.Value != null)
                context.State.AddHeader(key, _formatter.FormatHeaderValue(context.Value));

            return true;
        }
    }
}
