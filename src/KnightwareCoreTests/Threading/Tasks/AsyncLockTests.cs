using Knightware.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KnightwareCoreTests.Threading.Tasks
{
    [TestClass]
    public class AsyncLockTests
    {
        [TestMethod]
        public async Task AsyncLockReEntrancyTest()
        {
            var _lock = new AsyncLock();

            // The code below will be run immediately (likely in a new thread)
            var reentrancySucceeded = new TaskCompletionSource<bool>();
            Task<bool> t1 = Task.Run(async () =>
            {
                // A first call to LockAsync() will obtain the lock without blocking
                using (await _lock.LockAsync().ConfigureAwait(false))
                {
                    // A second call to LockAsync() will be recognized as being
                    // reentrant and permitted to go through without blocking.
                    using (await _lock.LockAsync().ConfigureAwait(false))
                    {
                        // Signal that we successfully entered both locks (reentrancy worked)
                        reentrancySucceeded.TrySetResult(true);
                        
                        // We now exclusively hold the lock for 5 seconds
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        return true;
                    }
                }
            });

            Task<bool> t2 = Task.Run(async() =>
            {
                // Wait a moment to ensure t1 starts first
                await Task.Delay(500).ConfigureAwait(false);
                // This call to obtain the lock will block until t1 has completed
                using (_lock.Lock())
                {
                    // Now we have obtained exclusive access.
                    // <Safely perform non-thread-safe operation safely here>
                    return true;
                }
            });

            await Task.WhenAny(Task.Delay(2000), reentrancySucceeded.Task).ConfigureAwait(false);
            Assert.IsTrue(reentrancySucceeded.Task.IsCompletedSuccessfully, "Task 1 appeared to get hung up trying to call LockAsync() reentrantly.");
            
            // Wait for both tasks to complete
            await Task.WhenAll(t1, t2).ConfigureAwait(false);
            Assert.IsTrue(t1.IsCompletedSuccessfully, "Task 1 did not complete successfully.");
            Assert.IsTrue(t2.IsCompletedSuccessfully, "Task 2 did not complete successfully.");

            // This call to obtain the lock is made synchronously from the main thread.
            // It will, however, block until the asynchronous code which obtained the lock
            // above finishes.
            using (_lock.Lock())
            {
                // Now we have obtained exclusive access.
                // <Safely perform non-thread-safe operation safely here>
            }
        }
    }
}
