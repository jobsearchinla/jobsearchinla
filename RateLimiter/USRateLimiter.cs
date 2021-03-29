using RateLimiter.Time;

namespace RateLimiter
{
    /// <summary>
    /// US rate limiter allows up to <requestLimit> request per <requestIntervalMs> milliseconds
    /// </summary>
    public class USRateLimiter : RateLimiter
    {
        public USRateLimiter(ITime dateTime, int requestLimit, int requestIntervalMs) : base(dateTime, requestLimit, requestIntervalMs)
        {
        }
    }
}
