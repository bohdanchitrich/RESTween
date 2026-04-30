using System;
using System.Collections;

namespace RESTween.Building
{
    public static class RestweenTypeUtilities
    {
        public static bool IsSimpleType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;

            if (actualType.IsPrimitive || actualType.IsEnum)
                return true;

            return actualType == typeof(string)
                   || actualType == typeof(decimal)
                   || actualType == typeof(DateTime)
                   || actualType == typeof(Guid);
        }

        public static bool IsQueryCollection(Type type)
        {
            if (type == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}
