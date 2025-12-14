using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class HeaderAttribute : Attribute
    {
        public string? Name { get; }

        public HeaderAttribute(string? name = null)
        {
            Name = name;
        }
    }
}
