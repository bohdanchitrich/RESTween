using System;
using System.Collections.Generic;
using System.Net.Http;

namespace RESTween.Building
{
    public sealed class RestweenRequestMetadata
    {
        public RestweenRequestMetadata(
            HttpMethod httpMethod,
            string urlTemplate,
            IReadOnlyList<Attribute> attributes,
            bool isMultipart)
        {
            HttpMethod = httpMethod;
            UrlTemplate = urlTemplate;
            Attributes = attributes;
            IsMultipart = isMultipart;
        }

        public HttpMethod HttpMethod { get; }

        public string UrlTemplate { get; }

        public IReadOnlyList<Attribute> Attributes { get; }

        public bool IsMultipart { get; }
    }
}
