using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Knightware.Threading.Tasks
{
    [TestClass]
    public class AsyncAutoResetEventTests
    {
        [TestMethod]
        public async Task IntervalTest()
        {
            var tcs = new TaskCompletionSource<bool>();
            var resetEvent = new AsyncAutoResetEvent();

            //Timeout in 100ms
            Task timeoutTask = resetEvent.WaitAsync(TimeSpan.FromMilliseconds(100)).ContinueWith((r) => tcs.TrySetResult(true));
            Task testTimeoutTask = Task.Delay(1000).ContinueWith((r) => tcs.TrySetResult(false));

            bool success = await tcs.Task;
            Assert.IsTrue(success, "Timeout for async reset event failed to fire");
        }
    }
}
