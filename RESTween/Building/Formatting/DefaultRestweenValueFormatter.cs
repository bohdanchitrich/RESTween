using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace RESTween.Building
{
    public sealed class DefaultRestweenValueFormatter : IRestweenValueFormatter
    {
        public string FormatRouteValue(object value)
        {
            return FormatQueryValue(value);
        }

        public string FormatQueryValue(object value)
        {
            switch (value)
            {
                case bool b:
                    return b ? "true" : "false";

                case DateTime dt:
                    return dt.Kind == DateTimeKind.Unspecified
                        ? dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                        : dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);

                case Enum e:
                    return GetEnumValue(e);

                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        public string FormatHeaderValue(object value)
        {
            return value switch
            {
                DateTime dt => dt.ToString("r", CultureInfo.InvariantCulture),
                _ => FormatQueryValue(value)
            };
        }

        private static string GetEnumValue(Enum value)
        {
            var member = value
                .GetType()
                .GetMember(value.ToString())
                .FirstOrDefault();

            var attr = member?.GetCustomAttribute<EnumMemberAttribute>();
            return attr?.Value ?? value.ToString();
        }
    }
}
