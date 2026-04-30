using System;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HeadersAttribute : Attribute
    {
        public string[] HeaderLines { get; }

        public HeadersAttribute(params string[] headerLines)
        {
            HeaderLines = headerLines ?? Array.Empty<string>();
        }
    }
}
