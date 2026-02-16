using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace RESTween.Handlers
{
    public sealed class RequestContext
    {
        internal RequestContext(HttpRequestMessage request, List<Attribute> attributes)
        {
            Request = request;
            Attributes = attributes;
        }

        public HttpRequestMessage Request { get; init; }
        public List<Attribute> Attributes { get; init; }

        public T? GetAttribute<T>() where T : Attribute
        {
            return Attributes.OfType<T>().FirstOrDefault();
        }

    }

}
