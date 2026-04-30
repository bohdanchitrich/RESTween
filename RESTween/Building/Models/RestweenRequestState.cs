using RESTween.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace RESTween.Building
{
    public sealed class RestweenRequestState
    {
        private readonly List<RestweenQueryValue> _queries = new List<RestweenQueryValue>();
        private object? _body;

        public RestweenRequestState(RestweenRequestMetadata metadata)
        {
            Metadata = metadata;
            Url = metadata.UrlTemplate;
            MultipartContent = metadata.IsMultipart ? new MultipartFormDataContent() : null;
        }

        public RestweenRequestMetadata Metadata { get; }

        public string Url { get; set; }

        public IDictionary<string, object> RouteValues { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        public IReadOnlyList<RestweenQueryValue> Queries => _queries;

        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public object? Body => _body;

        public bool HasBody { get; private set; }

        public MultipartFormDataContent? MultipartContent { get; }

        public void AddRoute(string name, object? value)
        {
            if (!Url.Contains("{" + name + "}"))
                throw new RestweenRequestBuildException($"Route {Url} not contain {name} parameter");

            if (value == null)
                throw new RestweenRequestBuildException($"Route parameter {name} in {Url} cannot be null");

            if (RouteValues.ContainsKey(name))
                throw new RestweenRequestBuildException($"{name} duplicated in {Url}");

            RouteValues[name] = value;
        }

        public void AddQuery(string name, object? value, CollectionFormat collectionFormat)
        {
            if (value == null)
                return;

            if (collectionFormat == CollectionFormat.Default
                && _queries.Any(q => q.Name.Equals(name, StringComparison.Ordinal)))
            {
                throw new RestweenRequestBuildException($"{name} duplicated in {Url}");
            }

            _queries.Add(new RestweenQueryValue(name, value, collectionFormat));
        }

        public void SetBody(object? value)
        {
            if (HasBody)
                throw new RestweenRequestBuildException($"Request {Url} can have only one body parameter");

            _body = value;
            HasBody = true;
        }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }
    }
}
