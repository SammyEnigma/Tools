using StackExchange.Redis;
using System;

namespace RedisTools
{
    internal static class RedisValueExtension
    {
        public static T As<T>(this RedisValue value)
        {
            // link: https://stackoverflow.com/questions/8171412/cannot-implicitly-convert-type-int-to-t
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }

    public static class TimeExtension
    {
        public static TimeSpan? ToTimeSpan(this DateTimeOffset? offset, bool utc = false)
        {
            if (offset == null)
                return null;

            if (utc)
                return offset.Value.UtcDateTime.Subtract(DateTime.UtcNow);
            return offset.Value.DateTime.Subtract(DateTime.Now);
        }

        public static TimeSpan ToTimeSpan(this DateTimeOffset offset, bool utc = false)
        {
            if (utc)
                return offset.UtcDateTime.Subtract(DateTime.UtcNow);
            return offset.DateTime.Subtract(DateTime.Now);
        }
    }
}
