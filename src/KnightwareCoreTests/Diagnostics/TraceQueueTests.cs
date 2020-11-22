using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Knightware.Diagnostics
{
    [TestClass]
    public class TraceQueueTests
    {
        [TestMethod]
        public void SetTracingLevelTest()
        {
            //Register for event notification
            TracingLevel eventLevel = TracingLevel.Error;
            bool eventFired = false;
            TraceQueue.TracingLevelChanged += (newLevel) =>
            {
                eventFired = true;
                eventLevel = newLevel;
            };

            //Change our way through tracing levels
            foreach (TracingLevel level in Enum.GetValues(typeof(TracingLevel)))
            {
                eventFired = false;
                TraceQueue.TracingLevel = level;
                Assert.AreEqual(level, TraceQueue.TracingLevel, "Level was not set correctly");
                Assert.IsTrue(eventFired, "TracingLevelChanged event did not fire");
                Assert.AreEqual(level, eventLevel, "TracingLevelChanged event fired, but with incorrect value");
            }
        }

        [TestMethod]
        public async Task TraceMessageTest()
        {
            var expectedLevel = TraceQueue.TracingLevel;
            string expectedMessage = "My Test trace";

            var tcs = new TaskCompletionSource<TraceMessage>();
            TraceQueue.TraceMessageRaised += (msg) => tcs.TrySetResult(msg);

            //Send a message
            TraceQueue.Trace(expectedLevel, expectedMessage);
            var actual = await tcs.Task;
            Assert.IsNotNull(actual, "Failed to receive trace notification");
            Assert.AreEqual(expectedLevel, actual.Level, "Level was incorrect");
            Assert.AreEqual(expectedMessage, actual.Message, "Message was incorrect");
        }
    }
}
