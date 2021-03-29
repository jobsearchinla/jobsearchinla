using RateLimiter.Time;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RateLimiter
{
    /// <summary>
    /// 
    /// The number of accepted requests are kept in a dictionary (see _dctRequests). The keys of _dctRequests are the time stamp 
    /// of the incoming request in ticks while the values correspond to the total number of accepted incoming requests at that given time stamp.
    /// 
    /// Every time a new requests comes in, the dictionary is refreshed: requests that were added in the past before the window length of the rate 
    /// limiter are dropped. 
    /// 
    /// The sum of all dictionary values gives the total number of requests accepted within current window. 
    /// If the sum has reached rate limiter capacity, the incoming request is rejected, otherwise, the incoming request is accepted
    /// and _dctRequests gets updated.
    /// 
    /// </summary>
    public class RateLimiter
    {
        private readonly object _syncObject = new object();

        private long? _startTime = null;

        /// <summary>
        /// Time Window 
        /// </summary>
        private readonly long _ticks;

        /// <summary>
        /// Capacity of requests per time window
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        ///  Keeps track of requests that were accepted to be serviced. 
        /// </summary>
        private Dictionary<long, int> _dctRequests = null;

        private readonly ITime _dateTime;


        public RateLimiter(ITime dateTime, int capacity, int requestIntervalMs)
        {
            if (capacity < 1)
            {
                throw new ArgumentException($"Capacity was set to {capacity}. It needs to be greater than zero.");
            }

            if (requestIntervalMs <= 0)
            {
                throw new ArgumentException($"Request Interval was set to {requestIntervalMs}. It needs to be greater than zero.");
            }

            _dateTime = dateTime;
            _capacity = capacity;
            _ticks = requestIntervalMs * TimeSpan.TicksPerMillisecond;

            _dctRequests = new Dictionary<long, int>();
        }

        private void DropExpiredRequests(long now)
        {
            var keys = _dctRequests.Keys.Where(k => k + _ticks <= now);
            if (keys != null)
            {
                foreach(var key in keys)
                {
                    _dctRequests.Remove(key);
                }
            }
        }

        private bool CanProcessIncomingRequest()
        {
            var total = _dctRequests.Sum(v => v.Value);
            return total < _capacity;
        }

        public bool RequestCanBeProcessed()
        {
            bool bValid = false;

            lock (_syncObject)
            {
                var now = _dateTime.GetTime();
                var elapsedTime = _startTime.HasValue ? (now - _startTime.Value) : 0;

                if (!_startTime.HasValue)
                {
                    // start timer
                    _startTime = now; 
                }
                else
                {
                    DropExpiredRequests(now);
                }


                bValid = CanProcessIncomingRequest();
                if (bValid == true)
                {
                    if (_dctRequests.ContainsKey(now))
                        _dctRequests[now]++;
                    else
                        _dctRequests[now] = 1;
                }
            }

            return bValid;
        }
    }
}
