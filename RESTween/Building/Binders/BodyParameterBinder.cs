using RESTween.Attributes;
using System.Reflection;

namespace RESTween.Building
{
    public sealed class BodyParameterBinder : IRestweenParameterBinder
    {
        public bool TryBind(RestweenParameterContext context)
        {
            if (context.Parameter.GetCustomAttribute<BodyAttribute>(true) != null)
            {
                context.State.SetBody(context.Value);
                return true;
            }

            if (context.Parameter.HasRestweenBindingAttribute())
                return false;

            if (!AllowsImplicitComplexBody(context.Metadata))
                return false;

            if (RestweenTypeUtilities.IsSimpleType(context.Parameter.ParameterType)
                || RestweenTypeUtilities.IsQueryCollection(context.Parameter.ParameterType))
            {
                return false;
            }

            context.State.SetBody(context.Value);
            return true;
        }

        private static bool AllowsImplicitComplexBody(RestweenRequestMetadata metadata)
        {
            return metadata.HttpMethod == System.Net.Http.HttpMethod.Post
                   || metadata.HttpMethod == System.Net.Http.HttpMethod.Put;
        }
    }
}
