using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTween.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RateLimitAttribute : Attribute
    {

        public int MaxRequests { get; }
        public int TimeWindowSeconds { get; }
        public string? Key { get; }


        public RateLimitAttribute(int maxRequests, int timeWindowSeconds)
        {
            if (maxRequests <= 0)
                throw new ArgumentException("MaxRequests must be greater than zero");

            if (timeWindowSeconds <= 0)
                throw new ArgumentException("TimeWindowSeconds must be greater than zero");

            MaxRequests = maxRequests;
            TimeWindowSeconds = timeWindowSeconds;
        }

        public RateLimitAttribute(int maxRequests, int timeWindowSeconds,string key)
        {
            if (maxRequests <= 0)
                throw new ArgumentException("MaxRequests must be greater than zero");

            if (timeWindowSeconds <= 0)
                throw new ArgumentException("TimeWindowSeconds must be greater than zero");

            MaxRequests = maxRequests;
            TimeWindowSeconds = timeWindowSeconds;
            Key = key;
        }
    }
}
