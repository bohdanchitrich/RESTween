using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute : Attribute
    {
        public string Url { get; }

        public PostAttribute(string url)
        {
            Url = url;
        }
    }
}
