using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Knightware.Threading.Tasks
{
    [TestClass]
    public class AsyncSemaphoreTests
    {
        [TestMethod]
        public async Task WaitAndReleaseTest()
        {
            var semaphore = new AsyncSemaphore(1);

            var task = semaphore.WaitAsync();
            Assert.IsTrue(task.IsCompleted, "First wait should complete immediately");

            semaphore.Release();
        }

        [TestMethod]
        public async Task MultipleWaitsBlockTest()
        {
            var semaphore = new AsyncSemaphore(1);

            await semaphore.WaitAsync();

            var secondWait = semaphore.WaitAsync();
            Assert.IsFalse(secondWait.IsCompleted, "Second wait should block");

            semaphore.Release();
            await Task.Delay(50);
            Assert.IsTrue(secondWait.IsCompleted, "Second wait should complete after release");
        }

        [TestMethod]
        public async Task InitialCountTest()
        {
            var semaphore = new AsyncSemaphore(3);

            var wait1 = semaphore.WaitAsync();
            var wait2 = semaphore.WaitAsync();
            var wait3 = semaphore.WaitAsync();

            Assert.IsTrue(wait1.IsCompleted);
            Assert.IsTrue(wait2.IsCompleted);
            Assert.IsTrue(wait3.IsCompleted);

            var wait4 = semaphore.WaitAsync();
            Assert.IsFalse(wait4.IsCompleted, "Fourth wait should block");

            semaphore.Release();
            await Task.Delay(50);
            Assert.IsTrue(wait4.IsCompleted);
        }

        [TestMethod]
        public void ReleaseWithoutWaitersIncreasesCount()
        {
            var semaphore = new AsyncSemaphore(1);

            semaphore.Release();
            semaphore.Release();

            var wait1 = semaphore.WaitAsync();
            var wait2 = semaphore.WaitAsync();
            var wait3 = semaphore.WaitAsync();

            Assert.IsTrue(wait1.IsCompleted);
            Assert.IsTrue(wait2.IsCompleted);
            Assert.IsTrue(wait3.IsCompleted);
        }
    }
}
