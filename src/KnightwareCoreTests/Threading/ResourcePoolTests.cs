using Knightware.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Knightware.Threading
{
    [TestClass]
    public class ResourcePoolTests
    {
        int incrementCount = 0;
        int decrementCount = 0;

        [TestInitialize]
        public void TestInitialize()
        {
            this.incrementCount = 0;
            this.decrementCount = 0;
        }

        [TestMethod]
        public async Task InitialSizeTest()
        {
            var config = new ResourcePoolConfig<ResourcePoolTestResource>()
            {
                InitialConnections = 50,
                MinimumConnections = 1,
                ResourceDeallocationInterval = TimeSpan.FromSeconds(10)
            };

            //Test fixture will test the initial size for us
            await RunResourcePoolTest(config, pool => Task.FromResult(true));
        }

        [TestMethod]
        public async Task DeallocateSizeTest()
        {
            var config = new ResourcePoolConfig<int>()
            {
                InitialConnections = 5,
                MinimumConnections = 1,
                ResourceDeallocationInterval = TimeSpan.FromMilliseconds(10)
            };

            await RunResourcePoolTest(config, async pool =>
            {
                //Wait for our pool resource count to shrink to the min level
                int expectedDecrementCount = config.InitialConnections - config.MinimumConnections;
                DateTime timeout = DateTime.Now.AddSeconds(10);
                while (decrementCount < expectedDecrementCount && DateTime.Now < timeout)
                {
                    await Task.Delay(100, TestContext.CancellationToken);
                }
                Assert.AreEqual(expectedDecrementCount, decrementCount, "Incorrect number of pool resources deallocated");
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Test will start with a single connection and build them up based on some test runs
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AllocateSizeTest()
        {
            var config = new ResourcePoolConfig<ResourcePoolTestResource>()
            {
                InitialConnections = 1,
                MinimumConnections = 1,
                MaximumConnections = 10,
                ResourceDeallocationInterval = TimeSpan.FromSeconds(10),
                ResourceAllocationInterval = TimeSpan.FromMilliseconds(10),
            };

            await RunResourcePoolTest(config, async pool =>
            {
                //Fire off multiple long runing tasks, which should cause our pool to spin up connections
                ManualResetEvent manualReset = new(false);

                var tasks = new List<Task>();
                for (int i = 0; i < config.MaximumConnections + 1; i++)
                {
                    tasks.Add(pool.RunOnThreadPool(val =>
                    {
                        //Code below will obtain the lock and then immediately release it when the function ends
                        Console.WriteLine($"Running on resource {val}");
                        manualReset.WaitOne();
                        return Task.FromResult(true);
                    }));
                }

                //Wait for the pool to spin up resources
                DateTime timeout = DateTime.Now.AddSeconds(30);
                while (incrementCount < config.MaximumConnections && DateTime.Now < timeout)
                {
                    await Task.Delay(100, TestContext.CancellationToken).ConfigureAwait(false);
                }
                if(DateTime.Now >= timeout)
                {
                    Assert.Fail("Timed out waiting for pool to allocate resources");
                }

                //Now check to see that we spun our max number of connections
                Assert.AreEqual(config.MaximumConnections, incrementCount, "Unexpected number of workers were created");

                //Unblock our workers
                manualReset.Set();

                //Let our workers complete
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task RunResourcePoolTest<T>(ResourcePoolConfig<T> config, Func<ResourcePool<T>, Task> runTest)
            where T: new()
        {
            var increment = new Func<Task<T>>(() =>
            {
                incrementCount++;
                return config.AddHandler?.Invoke();
            });

            var decrement = new Func<T, Task>((val) =>
            {
                decrementCount++;
                return config.RemoveHandler?.Invoke(val);
            });

            //Initialize pool with initial count
            var pool = new ResourcePool<T>()
            {
                InitialConnections = config.InitialConnections,
                MinimumConnections = config.MinimumConnections,
                MaximumConnections = config.MaximumConnections,
                ResourceDeallocationInterval = config.ResourceDeallocationInterval,
                ResourceAllocationInterval = config.ResourceAllocationInterval
            };
            Assert.IsTrue(await pool.StartupAsync(increment, decrement).ConfigureAwait(false), "Failed to start up");
            Assert.IsTrue(pool.IsRunning, "Pool is not running after startup");
            Assert.AreEqual(config.InitialConnections, incrementCount, "Expected number of items were not initialized");

            //Run some custom test(s) now that we're initialized
            await runTest(pool).ConfigureAwait(false);

            //Run Shutdown and ensure all items go away
            await pool.ShutdownAsync().ConfigureAwait(false);
            Assert.AreEqual(incrementCount, decrementCount, "Not all items had close called on them");
            Assert.IsFalse(pool.IsRunning, "Pool IsRunning still true after shutdown");
        }

        private class ResourcePoolConfig<T> where T: new()
        {
            public int InitialConnections { get; set; } = 5;
            public int MinimumConnections { get; set; } = 1;
            public int MaximumConnections { get; set; } = 5;
            public TimeSpan ResourceDeallocationInterval { get; set; } = TimeSpan.FromMilliseconds(100);
            public TimeSpan ResourceAllocationInterval { get; set; } = TimeSpan.FromMilliseconds(100);

            public Func<Task<T>> AddHandler { get; set; } = () => Task.FromResult(new T());

            public Func<T, Task> RemoveHandler { get; set; } = (t) => Task.FromResult(true);
        }

        private class ResourcePoolTestResource
        {
            private static int globallyUniqueId = 0;

            public int ConnectionID { get; }

            public ResourcePoolTestResource()
            {
                this.ConnectionID = Interlocked.Increment(ref globallyUniqueId);
            }

            public override bool Equals(object obj)
            {
                return obj is ResourcePoolTestResource resource &&
                       ConnectionID == resource.ConnectionID;
            }

            public override int GetHashCode()
            {
                return ConnectionID.GetHashCode();
            }
        }

        public TestContext TestContext { get; set; }
    }
}
