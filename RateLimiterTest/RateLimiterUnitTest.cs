using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using RateLimiter;
using RateLimiter.Time;

namespace RateLimiterTest
{
    [TestClass]
    public class RateLimiterUnitTest
    {
        private readonly Mock<ITime> _mockTime = new Mock<ITime>();

        private long GetElapsedTimeInTicks(int elapsedTimeMs)
        {
            return elapsedTimeMs * TimeSpan.TicksPerMillisecond;
        }


        /// <summary>
        /// EU Rate Limiter will serve the first incoming request
        /// </summary>
        [TestMethod]
        public void EU_Will_Serve_First_Incoming_Request()
        {
            var requestIntervalMs = 100;
            var rateLimiterWindow = new EURateLimiter(_mockTime.Object, requestIntervalMs);
;
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(requestIntervalMs / 2);

            Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
        }

        [TestMethod]
        public void US_Will_Serve_First_Incoming_Request()
        {
            var requestIntervalMs = 1000;
            var requestLimit = 10;
            var rateLimiterWindow = new USRateLimiter(_mockTime.Object,  requestLimit, requestIntervalMs);

            _mockTime
                .Setup(x => x.GetTime())
                .Returns(requestIntervalMs / 2);

            Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
        }


        /// <summary>
        /// Assume EU Rate Limiter with a time interval of 1000ms. 
        /// 50 requests are made within the first 1000ms. The first request
        /// will be accepted, while the other 49 will be rejected.
        /// </summary>
        [TestMethod]
        public void EU_Can_Process_Single_Request_Within_Time_Interval()
        {
            var requestIntervalMs = 1000;
            var requests = 50;
            var timespan = requestIntervalMs / requests;
            var incrementTicks = GetElapsedTimeInTicks(timespan); 
            var rateLimiterWindow = new EURateLimiter(_mockTime.Object, requestIntervalMs);

            // first request will be accepted and processed
            int nthRequest = 1;
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(nthRequest * incrementTicks);
            Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());

            // all requests that come within current window will be rejected
            while (nthRequest * timespan < requestIntervalMs)
            {
                nthRequest++;
                _mockTime
                    .Setup(x => x.GetTime())
                    .Returns(nthRequest * incrementTicks);
                Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());
            }

            // 
            nthRequest++;
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(nthRequest * incrementTicks);
            Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());

        }

        /// <summary>
        /// Assume US Rate Limiter with a time interval of 1000ms, with a capacity of 49 requests.
        /// 50 requests are made within the first 1000ms. All but the last one will be accepted.
        /// </summary>
        [TestMethod]
        public void US_Can_Process_Multiple_Requests_Within_Time_Interval()
        {
            var requestIntervalMs = 1000;
            var requests = 50;
            var capacity = requests - 1;
            var timespan = requestIntervalMs / requests;
            var incrementTicks = GetElapsedTimeInTicks(timespan); 
            var rateLimiterWindow = new USRateLimiter(_mockTime.Object,  capacity, requestIntervalMs);

            // all requests that come in in current window will be served if rate limiter has not reached capacity
            int nthRequest = 1;
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(nthRequest * incrementTicks);

            Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
            while (nthRequest < capacity)
            {
                nthRequest++;
                _mockTime
                    .Setup(x => x.GetTime())
                    .Returns(nthRequest * incrementTicks);
                Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
            }

            // capacity was reached and request will be rejected
            nthRequest++;
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(nthRequest * incrementTicks);
            Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());
        }

        /// <summary>
        /// Assume EU Rate Limiter with a time interval of 1000ms
        /// 50 requests are made concurrently within the first 1000ms. First one will be accepted and the rest will be rejected.
        /// </summary>
        [TestMethod]
        public void EU_Can_Process_One_From_Multiple_Concurrent_Requests_Within_Time_Interval()
        {
            var requestIntervalMs = 1000;
            var requests = 50;
            var incomingTime = GetElapsedTimeInTicks(requestIntervalMs/2);
            var rateLimiterWindow = new EURateLimiter(_mockTime.Object, requestIntervalMs);

            int nthRequest = 0;
            while (nthRequest < requests)
            {
                nthRequest++;
                _mockTime
                    .Setup(x => x.GetTime())
                    .Returns(incomingTime);

                // first request will be accepted
                if (nthRequest == 1)
                    Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
                else // rest of conccurent request will be rejected
                    Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());
            }
        }

        /// <summary>
        /// Assume US Rate Limiter with a time interval of 1000ms, with a capacity of 49 requests.
        /// 50 requests are made within the first 1000ms. Some of them come in concurrently (same time). 
        /// All but the last one will be accepted.
        /// </summary>
        [TestMethod]
        public void US_Can_Process_Multiple_Concurrent_Requests_Within_Time_Interval()
        {
            var requestIntervalMs = 1000;
            var requests = 50;
            var capacity = requests - 1;
            var timespan = requestIntervalMs / requests;
            var incrementTicks = GetElapsedTimeInTicks(timespan); 
            var rateLimiterWindow = new USRateLimiter(_mockTime.Object, capacity, requestIntervalMs);

            // all requests that come in in current window will be served if rate limiter has not reached capacity
            int iteration = 1;
            int nthRequest = 1;
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(nthRequest * incrementTicks);

            Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
            while (nthRequest < capacity)
            {
                nthRequest += 2;
                for (int i = 0; i < 2; i++)
                {
                    _mockTime
                        .Setup(x => x.GetTime())
                        .Returns(iteration * incrementTicks);
                    Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
                }

                iteration++;
            }

            // capacity was reached and request will be rejected
            nthRequest++;
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(nthRequest * incrementTicks);
            Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());
        }


        /// <summary>
        /// Assume EU Rate Limiter with a time interval of 1000ms
        /// 50 requests are made concurrently at time 'incomingTime' which is within the first 1000ms. First one will be accepted and the rest will be rejected.
        /// Request r1 comes it at '(incoming-1) + 1000ms' while request r2 comes in at 'incomingTime  + 10000ms'. The rate limiter will reject r1 but accept r2.
        /// </summary>
        [TestMethod]
        public void EU_Can_Process_Two_Requests_Within_Two_Different_Windows()
        {
            var requestIntervalMs = 1000;
            var firstRequestMs = requestIntervalMs / 2;
            var requests = 50;
            var timespan = requestIntervalMs / requests;
            var incrementTicks = GetElapsedTimeInTicks(timespan);
            var incomingTime = GetElapsedTimeInTicks(firstRequestMs);
            var rateLimiterWindow = new EURateLimiter(_mockTime.Object, requestIntervalMs);

            int nthRequest = 0;
            while (nthRequest < requests)
            {
                nthRequest++;
                _mockTime
                    .Setup(x => x.GetTime())
                    .Returns(incomingTime);

                // first request will be accepted
                if (nthRequest == 1)
                    Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
                else // rest of conccurent request will be rejected
                    Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());
            }


            // request will be rejected since we are within window that the limit has been reached
            var time = GetElapsedTimeInTicks(firstRequestMs + requestIntervalMs-1);
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(time);
            Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());

            // request will be accepted since we are in a new time window
            time = GetElapsedTimeInTicks(firstRequestMs + requestIntervalMs);
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(time + 1);
            Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
        }

        /// <summary>
        /// Assume US Rate Limiter with a time interval of 1000ms and capacity of 49 requests
        /// 50 requests are made concurrently at time 'incomingTime' which is within the first 1000ms. First 49 will be accepted and the 50th will be rejected.
        /// Request r1 comes it at '(incoming-1) + 1000ms' while request r2 and r3 come in at 'incomingTime  + 10000ms'. The rate limiter will reject r1 but accept r2 and r3.
        /// </summary>
        [TestMethod]
        public void US_Can_Process_Two_Requests_Within_Two_Different_Windows()
        {
            var requestIntervalMs = 1000;
            var firstRequestMs = requestIntervalMs / 2;
            var requests = 50;
            var timespan = requestIntervalMs / requests;
            var incrementTicks = GetElapsedTimeInTicks(timespan);
            var incomingTime = GetElapsedTimeInTicks(firstRequestMs);
            var rateLimiterWindow = new USRateLimiter(_mockTime.Object, requests-1, requestIntervalMs);

            int nthRequest = 0;
            while (nthRequest < requests-1)
            {
                nthRequest++;
                _mockTime
                    .Setup(x => x.GetTime())
                    .Returns(incomingTime);

                // first 49 requests will be accepted
                Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
            }
            // The 50th of the conccurent requests will be rejected
            Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());


            // request will be rejected since we are within window that the limit has been reached
            var time = GetElapsedTimeInTicks(firstRequestMs + requestIntervalMs - 1);
            _mockTime
                .Setup(x => x.GetTime())
                .Returns(time);
            Assert.IsFalse(rateLimiterWindow.RequestCanBeProcessed());

            // following two request will be accepted since we are in a new time window
            for (int i = 0; i < 2; i++)
            {
                time = GetElapsedTimeInTicks(firstRequestMs + requestIntervalMs);
                _mockTime
                    .Setup(x => x.GetTime())
                    .Returns(time + 1);
                Assert.IsTrue(rateLimiterWindow.RequestCanBeProcessed());
            }
        }

    }
}
