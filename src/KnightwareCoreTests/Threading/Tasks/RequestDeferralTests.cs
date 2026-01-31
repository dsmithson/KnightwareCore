using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knightware.Threading.Tasks
{
    [TestClass]
    public class RequestDeferralTests
    {
        [TestMethod]
        public async Task CompleteTest()
        {
            var deferral = new RequestDeferral();

            var waitTask = deferral.WaitForCompletedAsync();
            Assert.IsFalse(waitTask.IsCompleted);

            deferral.Complete();
            await waitTask;
            Assert.IsTrue(waitTask.IsCompleted);
        }

        [TestMethod]
        public async Task MultipleCompletesDoNotThrowTest()
        {
            var deferral = new RequestDeferral();

            deferral.Complete();
            deferral.Complete();

            await deferral.WaitForCompletedAsync();
        }

        [TestMethod]
        public async Task WaitForAllCompletedAsyncTest()
        {
            var deferrals = new List<RequestDeferral>
            {
                new RequestDeferral(),
                new RequestDeferral(),
                new RequestDeferral()
            };

            var waitTask = RequestDeferral.WaitForAllCompletedAsync(deferrals);
            Assert.IsFalse(waitTask.IsCompleted);

            deferrals[0].Complete();
            deferrals[1].Complete();
            Assert.IsFalse(waitTask.IsCompleted);

            deferrals[2].Complete();
            await waitTask;
            Assert.IsTrue(waitTask.IsCompleted);
        }

        [TestMethod]
        public async Task WaitForAllCompletedAsyncWithNullTest()
        {
            await RequestDeferral.WaitForAllCompletedAsync(null);
        }

        [TestMethod]
        public async Task WaitForAllCompletedAsyncWithEmptyListTest()
        {
            await RequestDeferral.WaitForAllCompletedAsync(new List<RequestDeferral>());
        }
    }
}
