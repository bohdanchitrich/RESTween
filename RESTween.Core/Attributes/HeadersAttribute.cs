using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HeadersAttribute : Attribute
    {
        public string[] HeaderLines { get; }
        public HeadersAttribute(params string[] headerLines) => HeaderLines = headerLines ?? Array.Empty<string>();
    }

}
