using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knightware.Threading.Tasks
{
    [TestClass]
    public class AutoResetWorkerTests
    {
        private int counter;

        [TestInitialize]
        public void TestInitialize()
        {
            counter = 0;
        }
        
        [TestMethod]
        public async Task WorkerStartupShutdown()
        {
            AutoResetWorker worker = new AutoResetWorker();
            Assert.IsTrue(await worker.StartupAsync(IncrementCounterWorker, null), "Failed to startup");
            Assert.IsTrue(worker.IsRunning, "IsRunning was false after starting up");

            //Run the worker
            worker.Set();
            await Task.Delay(100);

            //Shutdown the worker
            await worker.ShutdownAsync();
            Assert.IsFalse(worker.IsRunning, "Worker is reporting it is still running after shutdown");

            //Assert that one iteration of the worker successfully ran
            Assert.AreEqual(1, counter, "Unexpected counter value after shutting down worker");
        }

        private async Task IncrementCounterAsyncWorker(object state)
        {
            await Task.Run(() =>
                {
                    counter++;
                });
        }

        private Task IncrementCounterWorker(object state)
        {
            counter++;
            return Task.FromResult(true);
        }
    }
}
