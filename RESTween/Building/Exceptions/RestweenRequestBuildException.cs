using System;

namespace RESTween.Building
{
    public class RestweenRequestBuildException : InvalidOperationException
    {
        public RestweenRequestBuildException(string message)
            : base(message)
        {
        }

        public RestweenRequestBuildException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
