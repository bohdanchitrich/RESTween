using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Attributes
{
    public enum CollectionFormat
    {
        Default,
        Multi
    }


    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueryAttribute : Attribute
    {
        public string? Name { get; }
        public CollectionFormat CollectionFormat { get; }

        public QueryAttribute(
            string? name = null,
            CollectionFormat collectionFormat = CollectionFormat.Default)
        {
            Name = name;
            CollectionFormat = collectionFormat;
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
