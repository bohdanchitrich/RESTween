using System.IO;
using System.Net.Http;

namespace RESTween.Building
{
    public sealed class MultipartParameterBinder : IRestweenParameterBinder
    {
        private readonly IRestweenContentSerializer _serializer;

        public MultipartParameterBinder(IRestweenContentSerializer serializer)
        {
            _serializer = serializer;
        }

        public bool TryBind(RestweenParameterContext context)
        {
            if (!context.Metadata.IsMultipart)
                return false;

            var multipart = context.State.MultipartContent;
            if (multipart == null)
                return false;

            var name = context.Parameter.Name;
            if (name == null || context.Value == null)
                return true;

            if (context.Value is Stream stream)
            {
                multipart.Add(new StreamContent(stream), name, ResolveFileName(context));
                return true;
            }

            if (context.Value is byte[] bytes)
            {
                multipart.Add(new ByteArrayContent(bytes), name, ResolveFileName(context));
                return true;
            }

            if (context.Value is FileInfo fileInfo)
            {
                multipart.Add(new StreamContent(fileInfo.OpenRead()), name, fileInfo.Name);
                return true;
            }

            if (RestweenTypeUtilities.IsSimpleType(context.Parameter.ParameterType))
            {
                multipart.Add(new StringContent(context.Value.ToString() ?? string.Empty), name);
                return true;
            }

            multipart.Add(_serializer.SerializeMultipartJsonContent(context.Value), name);
            return true;
        }

        /// <summary>
        /// The multipart Content-Disposition filename for a Stream/byte[] part. A raw Stream carries
        /// no name of its own, so we look for a sibling "fileName" string parameter (the convention
        /// used across every Stream-upload method) and fall back to "file" if none was supplied.
        /// </summary>
        private static string ResolveFileName(RestweenParameterContext context)
        {
            if (context.TryGetSiblingValue("fileName", out var value) &&
                value is string fileName &&
                !string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }

            return "file";
        }
    }
}
