using RateLimiter.Time;

namespace RateLimiter
{
    /// <summary>
    /// EU rate limiter allows 1 request per <requestIntervalMs> milliseconds
    /// </summary>
    public class EURateLimiter : RateLimiter
    {
        public EURateLimiter(ITime time, int requestIntervalMs) : base(time,  1, requestIntervalMs)
        { 
        }
    }
}
