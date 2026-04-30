using System;

namespace RESTween.Attributes
{
    public enum CacheTimeUnit
    {
        Seconds,
        Minutes
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public int DurationSeconds { get; }

        public string? Key { get; }

        public CacheAttribute(int value, CacheTimeUnit unit = CacheTimeUnit.Seconds)
        {
            DurationSeconds = unit switch
            {
                CacheTimeUnit.Seconds => value,
                CacheTimeUnit.Minutes => value * 60,
                _ => value
            };
        }

        public CacheAttribute(int value, string key, CacheTimeUnit unit = CacheTimeUnit.Seconds)
        {
            DurationSeconds = unit switch
            {
                CacheTimeUnit.Seconds => value,
                CacheTimeUnit.Minutes => value * 60,
                _ => value
            };
            Key = key;
        }
    }
}
