using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knightware.Threading
{
    [TestClass]
    public class TimerTests
    {
        [TestMethod]
        public async Task DueTimeTest()
        {
            await runTest(100, Timeout.Infinite);
        }

        [TestMethod]
        public async Task IntervalTest()
        {
            await runTest(300, 300);
        }

        [TestMethod]
        public async Task ChangeTest()
        {
            int callCount = 0;

            //Run the timer for a second, then change it, and run it for another second to get a total count
            //Expect 9 iterations
            Timer timer = new Timer((s) => callCount++, null, 100, 100);
            await Task.Delay(1000);

            //Expect 4 iterations
            timer.Change(200, 200);
            await Task.Delay(1000);

            timer.Dispose();
            Assert.AreEqual(13, callCount, "Callcount was incorrect");
        }

        [TestMethod]
        public async Task ChangeTest2()
        {
            int callCount = 0;

            //Run the timer for a second, then change it, and run it for another second to get a total count
            //Expect 9 iterations
            Timer timer = new Timer((s) => callCount++, null, 100, 100);
            await Task.Delay(1000);

            //Expect no more iterations
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            int lastCallCount = callCount;
            await Task.Delay(1000);

            timer.Dispose();
            Assert.AreEqual(lastCallCount, callCount, "Timer continued to fire after being stopped");
        }

        [TestMethod]
        public async Task DisposeTest()
        {
            int callCount = 0;

            //Run the timer for a second, then change it, and run it for another second to get a total count
            //Expect 9 iterations
            Timer timer = new Timer((s) => callCount++, null, 100, 100);
            await Task.Delay(1000);

            //Expect no more iterations
            timer.Dispose();
            int lastCallCount = callCount;
            await Task.Delay(1000);

            timer.Dispose();
            Assert.AreEqual(lastCallCount, callCount, "Timer continued to fire after being stopped");
        }
        
        private async Task runTest(int dueTime, int interval)
        {
            int expectedCallCount;
            List<DateTime> callTimes = new List<DateTime>();
            DateTime startTime = DateTime.Now;

            Timer timer = new Timer((s) => callTimes.Add(DateTime.Now), null, dueTime, interval);

            int delayMs;
            if (dueTime < 0)
            {
                //Callback should never be called
                delayMs = 1000;
                expectedCallCount = 0;
            }
            else if (interval < 0)
            {
                //Should only be called once
                delayMs = (int)Math.Ceiling(2.5 * dueTime);
                expectedCallCount = 1;
            }
            else
            {
                //Give enough delay for about 5 intervals
                const int intervalCallbackCount = 5;
                delayMs = (int)Math.Ceiling(dueTime + (interval * intervalCallbackCount) + (interval * 0.5f));
                expectedCallCount = intervalCallbackCount + 1;
            }

            await Task.Delay(delayMs);

            //Ensure our callback count is correct
            Assert.AreEqual(expectedCallCount, callTimes.Count, "Callback count was not expected");

            //Ensure the interval was correct
            if (expectedCallCount > 0)
            {
                const int acceptableErrorMs = 100;
                DateTime lastCallTime = startTime;
                for (int i = 0; i < callTimes.Count; i++)
                {
                    DateTime callTime = callTimes[i];
                    int expectedMs = (i == 0 ? dueTime : interval);
                    int actualMs = (int)(callTime - lastCallTime).TotalMilliseconds;
                    Assert.IsTrue(Math.Abs(actualMs - expectedMs) < acceptableErrorMs, "Callback number {0} occurred outside of acceptable tolerance.  Expected {1}ms but was {2}ms",
                        i, expectedMs, actualMs);

                    //Setup for next iteration
                    lastCallTime = callTime;
                }
            }
        }
    }
}
