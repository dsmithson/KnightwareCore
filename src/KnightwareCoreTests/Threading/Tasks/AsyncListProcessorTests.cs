using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Threading.Tasks
{
    [TestClass]
    public class AsyncListProcessorTests
    {
        [TestMethod]
        public async Task StartupShutdownTest()
        {
            var processor = new AsyncListProcessor<AsyncListTestObject>(processItem);

            Assert.IsTrue(await processor.StartupAsync(), "Failed to startup list processor");
            Assert.IsTrue(processor.IsRunning, "IsRunning was false after startup");

            //Process some items
            var items = new List<AsyncListTestObject>();
            for (int i = 0; i < 10; i++)
            {
                var item = new AsyncListTestObject();
                items.Add(item);
                processor.Add(item);
            }
            Assert.IsTrue(Task.WaitAll(items.Select(item => item.Task).ToArray(), 10000), "Failed to process task lists");

            await processor.ShutdownAsync();
            Assert.IsFalse(processor.IsRunning, "IsRunning still true after shutdown");
        }

        private Task processItem(AsyncListProcessorItemEventArgs<AsyncListTestObject> e)
        {
            e.Item.SetResult(true);
            return Task.FromResult(true);
        }

        public class AsyncListTestObject
        {
            private TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            public Task<bool> Task { get { return tcs.Task; } }

            public void SetResult(bool result)
            {
                tcs.TrySetResult(result);
            }

        }
    }
}
