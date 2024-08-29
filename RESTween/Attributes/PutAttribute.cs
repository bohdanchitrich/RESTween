using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute : Attribute
    {
        public string Url { get; }

        public PutAttribute(string url)
        {
            Url = url;
        }
    }

   
}
