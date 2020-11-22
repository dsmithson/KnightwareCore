using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Knightware.Threading.Tasks
{
    [TestClass]
    public class BatchProcessorTests
    {
        private BatchProcessor<object, object> processor;

        public Task TestSimpleSetup(Func<object, object> itemHandler = null, Action onBatchProcessed = null, int minimumTimeIntervalMs = 1000, long maximumTimeIntervalMs = -1, int maximumCount = -1, UnprocessedItemAction unprocessedItemAction = UnprocessedItemAction.ReturnDefaultValue)
        {
            if (itemHandler == null)
                itemHandler = (req) => req;

            var batchHandler = new BatchHandler<object, object>(
                (items) =>
                {
                    foreach (var item in items)
                        item.SetResponse(itemHandler(item.Request));

                    if (onBatchProcessed != null)
                        onBatchProcessed();

                    return Task.FromResult(true);
                });

            return TestSimpleSetup(batchHandler, minimumTimeIntervalMs, maximumTimeIntervalMs, maximumCount, unprocessedItemAction);
        }

        public async Task TestSimpleSetup(BatchHandler<object, object> batchHandler, double minimumTimeIntervalMs = 1000, double maximumTimeIntervalMs = -1, int maximumCount = -1, UnprocessedItemAction unprocessedItemAction = UnprocessedItemAction.ReturnDefaultValue)
        {
            //Add an item to the queue, and then wait the minimum amount of time specified on the batch processor and check that our
            //item was processed
            processor = new BatchProcessor<object, object>();
            processor.UnprocessedItemAction = unprocessedItemAction;
            if (!await processor.StartupAsync(
                batchHandler,
                TimeSpan.FromMilliseconds(minimumTimeIntervalMs),
                (maximumTimeIntervalMs == -1 ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(maximumTimeIntervalMs)),
                maximumCount))
            {
                Assert.Inconclusive("Failed to startup batch processor");
            }
        }

        [TestMethod]
        public void TestCleanup()
        {
            //Generic cleanup
            if (processor != null)
            {
                processor.ShutdownAsync().Wait();
                processor = null;
            }
        }

        [TestMethod]
        public async Task MinimumElapsedTest()
        {
            int timeoutMs = 1000;

            await TestSimpleSetup(minimumTimeIntervalMs: timeoutMs);

            //Add an item, and then ensure the response took at least the timeout time
            Stopwatch stopwatch = Stopwatch.StartNew();
            object result = await processor.EnqueueAsync(5);
            stopwatch.Stop();

            Assert.IsTrue(stopwatch.Elapsed.TotalMilliseconds > timeoutMs, "Not enough time passed before patch was processed");
            Console.WriteLine("Item was processed in {0} - timeout was configured for {1} milliseconds", stopwatch.Elapsed, timeoutMs);
        }

        [TestMethod]
        public async Task MinimumElapsedTest2()
        {
            //Add a few items to the queue, and then wait the minimum amount of time specified on the batch processor and check that 
            //both items were processed as a single batch
            int timeoutMs = 3000;
            const int expectedBatchID = 1;
            int batchID = expectedBatchID;

            await TestSimpleSetup(
                itemHandler: (req) => batchID,
                onBatchProcessed: () => batchID++, //Increment the batch ID after each batch gets processed
                minimumTimeIntervalMs: timeoutMs);

            ///Enqueue a batch of 3 items and time their completion
            var results = new List<Task<object>>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            results.Add(processor.EnqueueAsync(5));
            results.Add(processor.EnqueueAsync(6));
            results.Add(processor.EnqueueAsync(7));
            await Task.WhenAll(results.ToArray());
            stopwatch.Stop();

            //Verify we waited the minimum amount of time and that all items were processed in the same batch
            Assert.IsTrue(results.All(r => (int)r.Result == expectedBatchID), "One or more items were not processed in the same batch as the others");
            Assert.IsTrue(stopwatch.Elapsed.TotalMilliseconds > timeoutMs, "Not enough time passed before patch was processed");
            Console.WriteLine("Items were processed in {0} - timeout was configured for {1} milliseconds", stopwatch.Elapsed, timeoutMs);
        }

        [TestMethod]
        public async Task MaximumElapsedTest()
        {
            //Add items to the queue at a rate 10ms faster than the minimum elapsed time to continuously reset the timer, and after the
            //maximum elspaed time has passed ensure that a batch of items is processed
            int minimumTimeoutMs = 1000;
            int maximumTimeoutMs = 3000;
            var stopwatch = new Stopwatch();

            await TestSimpleSetup(
                onBatchProcessed: () => stopwatch.Stop(), //Signal that our batch has been processed
                minimumTimeIntervalMs: minimumTimeoutMs,
                maximumTimeIntervalMs: maximumTimeoutMs);

            //Add an item 100 ms before the minimum timeout elapses to force our maximum timeout to hit
            TimeSpan addInterval = processor.MinimumTimeInterval.Subtract(TimeSpan.FromMilliseconds(100));
            TimeSpan testTimeout = TimeSpan.FromMilliseconds(processor.MaximumTimeInterval.TotalMilliseconds * 2);
            stopwatch.Start();
            int itemCount = 0;
            while (stopwatch.IsRunning)
            {
                //Sanity check
                if (stopwatch.Elapsed > testTimeout)
                    Assert.Fail("Timed out waiting for test to complete");

                //Add an item and wait
                Task t = processor.EnqueueAsync(itemCount++);
                await Task.Delay(addInterval);
            }

            //Verify we waited the maximum amount of time before the first batch was processed
            Assert.IsTrue(stopwatch.Elapsed.TotalMilliseconds > maximumTimeoutMs, "Not enough time passed {0} before patch was processed", stopwatch.Elapsed);
            Console.WriteLine("Items were processed in {0} - timeout was configured for {1} milliseconds", stopwatch.Elapsed, maximumTimeoutMs);
        }

        [TestMethod]
        public async Task MaximumCountTest()
        {
            //Set a maximum number of items on our batch processor, and add them before the minimum time limit.  Verify that we send the batch
            //when we hit the count instead of waiting for the minimum time to elapse
            const int maxCount = 5;
            const int maxTime = 5000;
            const int minTime = 5000;
            int itemsProcessed = 0;
            var stopwatch = new Stopwatch();

            await TestSimpleSetup(
                itemHandler: (req) => itemsProcessed++,
                onBatchProcessed: () => stopwatch.Stop(), //Signal that our batch has been processed
                maximumCount: maxCount,
                minimumTimeIntervalMs: minTime,
                maximumTimeIntervalMs: maxTime);

            //Add items every 10 ms until the stopwatch stops, which should be roughly when we hit our max count
            TimeSpan addInterval = TimeSpan.FromMilliseconds(10);
            TimeSpan testTimeout = TimeSpan.FromMilliseconds(processor.MaximumTimeInterval.TotalMilliseconds * 2);
            stopwatch.Start();
            while (stopwatch.IsRunning)
            {
                //Sanity check
                if (stopwatch.Elapsed > testTimeout)
                    Assert.Fail("Timed out waiting for test to complete");

                //Add an item and wait
                Task t = processor.EnqueueAsync(0);
                await Task.Delay(addInterval);
            }

            //Verify we waited the maximum amount of time before the first batch was processed
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            Assert.IsTrue(elapsedMs < minTime && elapsedMs < maxTime, "Batch timed out on time intervals, not max count");
            Assert.AreEqual(maxCount, itemsProcessed, "Incorrect number of items processed in the batch");
        }

        [TestMethod]
        public async Task MaximumCountMultipleTest()
        {
            //Runs through multiple iterations of a maximum count test on a single instance of the item processor to ensure it works correctly
            //when hitting the max items limit multiple times
            const int maxItemsPerBatch = 15;
            var results = await RunContinuousTestAsync(TimeSpan.FromSeconds(10), 10, 5000, 5000, maxItemsPerBatch);
            Assert.IsTrue(results.Count > 1, "Expected more items");

            for (int i = 0; i < results.Count; i++)
            {
                if (i == (results.Count - 1))
                    Assert.IsTrue(results[i].Item2 <= maxItemsPerBatch, "Invalid number of items in last batch");
                else
                    Assert.AreEqual(maxItemsPerBatch, results[i].Item2, "Incorrect number of items in batch");
            }
        }

        [TestMethod]
        public void MaximumElapsedMultipleTest()
        {
            //Runs through multiple iterations of a maximum elapsed timeout test on a single instance of the item processor to ensure it works correctly
            //when hitting the max timeout limit multiple times

        }

        [TestMethod]
        public async Task MinimumElapsedMultipleTest()
        {
            //Runs through multiple iterations of a minimum elapsed timeout test on a single instance of the item processor to ensure it works correctly
            //when hitting the min timeout limit multiple times
            const int minMs = 100;
            const int expectedBatches = 5;
            const int expectedItemsPerBatch = 1;

            var batchesSizesProcessed = new List<int>();
            int itemsProcessedInCurrentBatch = 0;

            //Setup a handler that will increment our batch process count per item and add the batch count and a timestamp when a batch completes
            await TestSimpleSetup(
                itemHandler: (req) =>
                {
                    itemsProcessedInCurrentBatch++;
                    return req;
                },
                onBatchProcessed: () =>
                {
                    batchesSizesProcessed.Add(itemsProcessedInCurrentBatch);
                    itemsProcessedInCurrentBatch = 0;
                },
                minimumTimeIntervalMs: minMs,
                maximumTimeIntervalMs: 10000,
                maximumCount: int.MaxValue);

            //Run our test duration, adding items as needed
            for (int i = 0; i < expectedBatches; i++)
            {
                //We'll add a couple items, then wait for our min refresh time to elapse
                for (int j = 0; j < expectedItemsPerBatch; j++)
                {
                    Task t1 = processor.EnqueueAsync(0);
                }
                await Task.Delay(minMs * 2);
            }

            //Wait for last batch to finish...
            await Task.Delay(1000);

            //Verify we processed the correct number of batches
            Assert.AreEqual(expectedBatches, batchesSizesProcessed.Count, "Incorrect number of batches processed");

            foreach (int batchSize in batchesSizesProcessed)
                Assert.AreEqual(expectedItemsPerBatch, batchSize, "Incorrect batch size processed");
        }

        [TestMethod]
        public async Task ProcessImmediateTest()
        {
            //Verifies that the queue is immediately processed when the processImmediate flag is set during item queueing
            int batchSize = 0;
            await TestSimpleSetup(
                itemHandler: (req) => batchSize++,
                minimumTimeIntervalMs: 1000,
                maximumTimeIntervalMs: 3000);

            //We'll add an item to the queue with the normal batching behavior, then add an immediate which should force the queue to process immediately
            Task t1 = processor.EnqueueAsync(0);
            await Task.Delay(100);

            var stopWatch = Stopwatch.StartNew();
            var immediateResult = await processor.EnqueueAsync(2, true);
            stopWatch.Stop();

            Assert.AreEqual(2, batchSize, "Unexpected batch size processed");
            Assert.IsTrue(stopWatch.Elapsed < processor.MinimumTimeInterval, "Immediate item didn't appear to be processed immediately.  Processed in {0}ms", stopWatch.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Runs the batch processor for a specified amount of time, and returns a list of tuples with the time of a batch processing and the number of items processed
        /// </summary>
        private async Task<List<Tuple<TimeSpan, int>>> RunContinuousTestAsync(TimeSpan testTime, int itemAddIntervalMs, int minMs, int maxMs, int maxCount)
        {
            var response = new List<Tuple<TimeSpan, int>>();
            int itemsProcessedInCurrentBatch = 0;
            Stopwatch stopWatch = new Stopwatch();

            //Setup a handler that will increment our batch process count per item and add the batch count and a timestamp when a batch completes
            await TestSimpleSetup(
                itemHandler: (req) =>
                {
                    itemsProcessedInCurrentBatch++;
                    return req;
                },
                onBatchProcessed: () =>
                {
                    response.Add(new Tuple<TimeSpan, int>(stopWatch.Elapsed, itemsProcessedInCurrentBatch));
                    itemsProcessedInCurrentBatch = 0;
                },
                minimumTimeIntervalMs: minMs,
                maximumTimeIntervalMs: maxMs,
                maximumCount: maxCount);

            //Run our test duration, adding items as needed
            stopWatch.Start();
            while (stopWatch.Elapsed < testTime)
            {
                Task t = processor.EnqueueAsync(0);
                await Task.Delay(itemAddIntervalMs);
            }
            stopWatch.Stop();
            return response;
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task UnprocessedItemExceptionTest()
        {
            //Set our unprocessed item action to throw exception, add an item to the queue and in our worker don't process it.  Verify an
            //exception is thrown
            await TestSimpleSetup(
                batchHandler: (items) => Task.FromResult(true), //Don't actually process the items
                minimumTimeIntervalMs: 10,
                unprocessedItemAction: UnprocessedItemAction.ThrowException);

            //Add an item, and then ensure the response took at least the timeout time
            object result = await processor.EnqueueAsync(5);
        }

        [TestMethod]
        public async Task UnprocessedItemDefaultValueTest()
        {
            //Set our unprocessed item action to return default value, and add an item to the queue and in our worker don't process it.  Verify
            //the default value is set on our task

            //Set our unprocessed item action to throw exception, add an item to the queue and in our worker don't process it.  Verify an
            //exception is thrown
            await TestSimpleSetup(
                batchHandler: (items) => Task.FromResult(true), //Don't actually process the items
                minimumTimeIntervalMs: 10,
                unprocessedItemAction: UnprocessedItemAction.ReturnDefaultValue);

            //Add an item, and then ensure the response took at least the timeout time
            object result = await processor.EnqueueAsync(5);
            Assert.IsNull(result, "Expected result to be null");
        }
    }
}
