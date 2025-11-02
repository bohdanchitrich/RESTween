using RESTween.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Core
{
    public static class MethodIdGenerator
    {


        public static (string HttpMethod, string Url)? GetHttpMetadata(MethodInfo method)
        {
            if (method.GetCustomAttribute<GetAttribute>() is GetAttribute getAttr)
                return ("GET", getAttr.Url);
            if (method.GetCustomAttribute<PostAttribute>() is PostAttribute postAttr)
                return ("POST", postAttr.Url);
            if (method.GetCustomAttribute<PutAttribute>() is PutAttribute putAttr)
                return ("PUT", putAttr.Url);
            if (method.GetCustomAttribute<DeleteAttribute>() is DeleteAttribute deleteAttr)
                return ("DELETE", deleteAttr.Url);

            return null;
        }


        public static Guid Create(MethodInfo method)
        {
            var meta = GetHttpMetadata(method);
            if (meta == null)
                throw new InvalidOperationException($"Method {method.Name} has no HTTP attribute.");

            var (httpMethod, url) = meta.Value;

            var route = url.Trim('/').ToLowerInvariant();
            var paramTypes = string.Join(",",
                method.GetParameters().Select(p => p.ParameterType.FullName));

            var signature = $"{httpMethod}:{route}:{paramTypes}";

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(signature));
            return new Guid(hash.Take(16).ToArray());
        }
    }

}
