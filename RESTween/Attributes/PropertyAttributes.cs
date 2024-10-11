using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueryAttribute : Attribute
    {
        public string? Name { get; }

        public QueryAttribute(string name)
        {
            Name = name;
        }
        public QueryAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class RouteAttribute : Attribute
    {
        public string? Name { get; }

        public RouteAttribute(string name)
        {
            Name = name;
        }
        public RouteAttribute()
        {

        }
    }



    [AttributeUsage(AttributeTargets.Parameter)]
    public class BodyAttribute : Attribute
    {
    }
}
