using RESTween.Attributes;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace RESTween.Building
{
    public sealed class QueryParameterBinder : IRestweenParameterBinder
    {
        public bool TryBind(RestweenParameterContext context)
        {
            var parameterName = context.Parameter.Name;
            if (parameterName == null)
                return true;

            var queryAttr = context.Parameter.GetCustomAttribute<QueryAttribute>(true);
            if (queryAttr != null)
            {
                var queryName = queryAttr.Name ?? parameterName;
                BindQueryValue(context, queryName, queryAttr.CollectionFormat, forceQuery: true);
                return true;
            }

            if (context.Parameter.HasRestweenBindingAttribute())
                return false;

            if (RestweenTypeUtilities.IsSimpleType(context.Parameter.ParameterType)
                || RestweenTypeUtilities.IsQueryCollection(context.Parameter.ParameterType))
            {
                context.State.AddQuery(parameterName, context.Value, CollectionFormat.Default);
                return true;
            }

            if (!AllowsImplicitComplexBody(context.Metadata))
            {
                ExpandObjectToQuery(context, context.Value);
                return true;
            }

            return false;
        }

        private static void BindQueryValue(
            RestweenParameterContext context,
            string queryName,
            CollectionFormat collectionFormat,
            bool forceQuery)
        {
            if (RestweenTypeUtilities.IsSimpleType(context.Parameter.ParameterType)
                || RestweenTypeUtilities.IsQueryCollection(context.Parameter.ParameterType))
            {
                context.State.AddQuery(queryName, context.Value, collectionFormat);
                return;
            }

            if (forceQuery)
                ExpandObjectToQuery(context, context.Value);
        }

        private static void ExpandObjectToQuery(RestweenParameterContext context, object? value)
        {
            if (value == null)
                return;

            var props = value.GetType()
                .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod?.IsPublic == true || p.GetMethod?.IsAssembly == true)
                .ToArray();

            foreach (var prop in props)
            {
                var propValue = prop.GetValue(value);
                if (propValue == null)
                    continue;

                var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>(true)?.Name;
                context.State.AddQuery(jsonName ?? prop.Name, propValue, CollectionFormat.Default);
            }
        }

        private static bool AllowsImplicitComplexBody(RestweenRequestMetadata metadata)
        {
            return metadata.HttpMethod == System.Net.Http.HttpMethod.Post
                   || metadata.HttpMethod == System.Net.Http.HttpMethod.Put;
        }
    }
}
