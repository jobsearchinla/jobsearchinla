using System.Diagnostics;

namespace RateLimiter.Time
{
    public class Time : ITime
    {
        public Time()
        {
        }

        public long GetTime()
        {
            return Stopwatch.GetTimestamp();
        }
    }
}
