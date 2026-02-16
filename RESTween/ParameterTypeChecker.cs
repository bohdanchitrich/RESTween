using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween
{
    internal static class ParameterTypeChecker
    {
        internal static bool IsSimpleType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            if (type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(Guid))
            {
                return true;
            }

            return false;
        }

        internal static bool IsQueryCollection(Type type)
        {
            if (type == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(type);
        }

    }
}
