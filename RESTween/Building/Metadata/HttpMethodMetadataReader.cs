using RESTween.Attributes;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace RESTween.Building
{
    public sealed class HttpMethodMetadataReader
    {
        public RestweenRequestMetadata Read(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var attributes = method.GetCustomAttributes<Attribute>(true).ToList();
            var isMultipart = method.GetCustomAttribute<MultipartAttribute>(true) != null;

            if (method.GetCustomAttribute<GetAttribute>(true) is GetAttribute getAttr)
                return new RestweenRequestMetadata(HttpMethod.Get, getAttr.Url, attributes, isMultipart);

            if (method.GetCustomAttribute<PostAttribute>(true) is PostAttribute postAttr)
                return new RestweenRequestMetadata(HttpMethod.Post, postAttr.Url, attributes, isMultipart);

            if (method.GetCustomAttribute<PutAttribute>(true) is PutAttribute putAttr)
                return new RestweenRequestMetadata(HttpMethod.Put, putAttr.Url, attributes, isMultipart);

            if (method.GetCustomAttribute<DeleteAttribute>(true) is DeleteAttribute deleteAttr)
                return new RestweenRequestMetadata(HttpMethod.Delete, deleteAttr.Url, attributes, isMultipart);

            throw new RestweenRequestBuildException("Only GET, POST, PUT, and DELETE methods are supported.");
        }
    }
}
