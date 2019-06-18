using Knightware.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace Knightware.Threading.Tasks
{
    /// <summary>
    /// Determines a cleanup action to be performed on unprocessed items
    /// </summary>
    public enum UnprocessedItemAction { ReturnDefaultValue, ThrowException }

    public delegate Task BatchHandler<TRequest, TResponse>(IReadOnlyCollection<IBatchProcessorRequest<TRequest, TResponse>> batchItems);

    /// <summary>
    /// Takes synchronous and asynchronous incoming requests, batches them, and dispatches them based on defined time boundaries
    /// </summary>
    /// <typeparam name="TRequest">Type of object being added to the processor for batching</typeparam>
    /// <typeparam name="TResponse">Type of object to be returned when batch item processing has completed</typeparam>
    public class BatchProcessor<TRequest, TResponse>
    {
        private Timer batchTimer;
        private Stopwatch queueFirstItemStopwatch;
        private Stopwatch queueLatestItemStopwatch;
        private List<InternalBatchProcessorItem> queue;
        private readonly AsyncLock queueLock = new AsyncLock();
        private AsyncListProcessor<List<InternalBatchProcessorItem>> batchProcessor;
        private BatchHandler<TRequest, TResponse> userBatchProcessorHandler;

        /// <summary>
        /// Determines what action should be performed automatically on queued items which are not completed during batch processing
        /// </summary>
        public UnprocessedItemAction UnprocessedItemAction { get; set; } = UnprocessedItemAction.ThrowException;

        /// <summary>
        /// Gets/Sets the minimum amount of time which must elapse after a request is received before reaching a batch cut-off.  This time span will reset with each incoming request.
        /// </summary>
        public TimeSpan MinimumTimeInterval { get; set; }

        /// <summary>
        /// Gets/Sets the maximum amount of time which can elapse from the time the first request in a batch is received and the amount of time before it's associated batch is processed
        /// </summary>
        public TimeSpan MaximumTimeInterval { get; set; }

        /// <summary>
        /// Gets/Sets the maximum number of requests that can be included in a single batch.  Once this number is queued, a batch will be processed regardless of the time elapsed
        /// </summary>
        public int MaximumCount { get; set; }

        public bool IsRunning { get; private set; }

        public Task<bool> StartupAsync(BatchHandler<TRequest, TResponse> batchHandler, TimeSpan minimumTimeInterval)
        {
            return StartupAsync(batchHandler, minimumTimeInterval, TimeSpan.MaxValue);
        }

        public Task<bool> StartupAsync(BatchHandler<TRequest, TResponse> batchHandler, TimeSpan minimumTimeInterval, TimeSpan maximumTimeInterval)
        {
            return StartupAsync(batchHandler, minimumTimeInterval, maximumTimeInterval, -1);
        }

        public async Task<bool> StartupAsync(BatchHandler<TRequest, TResponse> batchHandler, TimeSpan minimumTimeInterval, TimeSpan maximumTimeInterval, int maximumCount)
        {
            await ShutdownAsync();

            if(minimumTimeInterval > maximumTimeInterval)
                throw new ArgumentException("Maximum time interval cannot be less than the minimum time interval");

            if(minimumTimeInterval <= TimeSpan.Zero)
                throw new ArgumentException("Minimum time interval must be greater than zero");
            
            this.userBatchProcessorHandler = batchHandler;
            this.MinimumTimeInterval = minimumTimeInterval;
            this.MaximumTimeInterval = maximumTimeInterval;
            this.MaximumCount = maximumCount;
            IsRunning = true;
            
            batchProcessor = new AsyncListProcessor<List<InternalBatchProcessorItem>>(batchProcessor_DoWork, () => IsRunning);
            if (!await batchProcessor.StartupAsync())
            {
                TraceQueue.Trace(this, TracingLevel.Warning, "Failed to startup batch processor.  Shutting down...");
                await ShutdownAsync();
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Shutdown the current batch processor
        /// </summary>
        /// <param name="maxWait">Maximum amount of time, in milliseconds, to wait for any pending operation to finish</param>
        /// <returns></returns>
        public async Task ShutdownAsync(int maxWait = -1)
        {
            IsRunning = false;

            using (var releaser = await queueLock.LockAsync())
            {
                if (batchTimer != null)
                {
                    batchTimer.Change(-1, -1);
                    batchTimer.Dispose();
                    batchTimer = null;
                }
            }

            if (batchProcessor != null)
            {
                await batchProcessor.ShutdownAsync(maxWait);
                batchProcessor = null;
            }

            queue = null;
        }

        /// <summary>
        /// Adds a new request to the batch processor for processing
        /// </summary>
        /// <param name="request">Request to be processed</param>
        /// <param name="processImmediate">When set to true, the current batch will be processed as soon as the request is enqueued</param>
        /// <returns>A Task of TResponse which will be completed when the item has been processed</returns>
        public async Task<TResponse> EnqueueAsync(TRequest request, bool processImmediate = false)
        {
            if(request == null)
                return default(TResponse);

            var newItem = new InternalBatchProcessorItem(request);

            using (var releaser = await queueLock.LockAsync().ConfigureAwait(false))
            {
                if(queue == null)
                    queue = new List<InternalBatchProcessorItem>();

                queue.Add(newItem);

                queueLatestItemStopwatch = Stopwatch.StartNew();
                if (processImmediate || (this.MaximumCount > 0 && queue.Count >= this.MaximumCount))
                {
                    //We've hit our maximum count - fire our batch process now and reset the current queue
                    batchProcessor.Add(queue);
                    queue = new List<InternalBatchProcessorItem>();

                    queueLatestItemStopwatch.Reset();
                    queueFirstItemStopwatch.Reset();
                }
                else if (queue.Count == 1)
                {
                    //Begin our monitoring timer
                    queueFirstItemStopwatch = queueLatestItemStopwatch;
                    batchTimer = new Timer((state) =>
                    {
                        Task t = OnTimerElapsed(state);
                    }, 
                    null, this.MinimumTimeInterval, this.MinimumTimeInterval);
                }
            }

            return await newItem.Task;
        }

        /// <summary>
        /// Adds a new request to the batch processor for processing.  Method will block until the item has been processed - recommended to use EnqueueAsync where possible
        /// </summary>
        /// <param name="request">Request to be processed</param>
        /// <param name="processImmediate">When set to true, the current batch will be processed as soon as the request is enqueued</param>
        /// <returns>A processed response object</returns>
        public TResponse Enqueue(TRequest request, bool processImmediate = false)
        {
            return EnqueueAsync(request).Result;
        }

        /// <summary>
        /// Worker for the timer which fires at the MinimumTimeInterval which sends the current queue to be processed when min/max elapsed time requirements met
        /// </summary>
        /// <param name="state"></param>
        private async Task OnTimerElapsed(object state)
        {
            //Stop the timer while we process the current iteration
            batchTimer.Change(-1, -1);
            bool restartTimer = true;

            using (var releaser = await queueLock.LockAsync())
            {
                //Has the minimum amount of time elapsed since our last item or the maximum amount of time elapsed since the first item was added?
                bool minTimeHasElapsed = queueLatestItemStopwatch.Elapsed > MinimumTimeInterval;
                bool maxTimeElapsed = queueFirstItemStopwatch.Elapsed > MaximumTimeInterval;
                if (minTimeHasElapsed || maxTimeElapsed)
                {
                    //Fire off the current batch group to be processed.  Since we're emptying the list of batch items, no need to restart the timer
                    batchProcessor.Add(queue);
                    queue = new List<InternalBatchProcessorItem>();
                    restartTimer = false;

                    queueLatestItemStopwatch.Reset();
                    queueFirstItemStopwatch.Reset();
                }
            }

            //Restart
            if (restartTimer)
                batchTimer.Change(MinimumTimeInterval, MinimumTimeInterval);
        }

        /// <summary>
        /// This worker method handles wrapping a batch of items and fire the user-provided batch handler, and handles cleanup of any uncompleted batch items
        /// </summary>
        /// <param name="e">List of items included in the batch</param>
        /// <returns></returns>
        private async Task batchProcessor_DoWork(AsyncListProcessorItemEventArgs<List<InternalBatchProcessorItem>> e)
        {
            var batch = e.Item;

            try
            {
                //Create a read-only collection to send to the user-provided handler
                var userBatch = batch.Select(item => new BatchProcessorRequest(item)).ToList();
                var readOnlyUserBatch = new ReadOnlyCollection<BatchProcessorRequest>(userBatch);
                await userBatchProcessorHandler(readOnlyUserBatch);
            }
            catch (Exception ex)
            {
                TraceQueue.Trace(this, TracingLevel.Warning, "{0} occurred while processing batch: {1}", ex.GetType().Name, ex.Message);
            }
            finally
            {
                //Force any item left uncompleted into either an exception or default response
                foreach (var item in batch)
                {
                    if (!item.Task.IsCompleted)
                    {
                        if(this.UnprocessedItemAction == UnprocessedItemAction.ThrowException)
                            item.TaskSource.TrySetException(new TaskCanceledException("Task was not processed by batch processor handler"));
                        else
                            item.TaskSource.TrySetResult(default(TResponse));
                    }
                }
            }
        }
        
        /// <summary>
        /// Contains a request item to be processed by a batch processor handler
        /// </summary>
        public class BatchProcessorRequest : IBatchProcessorRequest<TRequest, TResponse>
        {
            private readonly InternalBatchProcessorItem source;
            public TRequest Request { get; set; }

            internal BatchProcessorRequest(InternalBatchProcessorItem source)
            {
                this.source = source;
            }

            public void SetResponse(TResponse response)
            {
                this.source.TaskSource.TrySetResult(response);
            }
        }

        /// <summary>
        /// Internal state management task for a single request item
        /// </summary>
        internal class InternalBatchProcessorItem
        {
            public TRequest Request { get; private set; }
            public TaskCompletionSource<TResponse> TaskSource { get; private set; }
            public Task<TResponse> Task { get { return TaskSource.Task; } }
            public DateTime EnqueueTime { get; private set; }

            public InternalBatchProcessorItem(TRequest request)
            {
                this.Request = request;
                this.EnqueueTime = DateTime.UtcNow;
                this.TaskSource = new TaskCompletionSource<TResponse>();
            }
        }
    }

    public interface IBatchProcessorRequest<TRequest, in TResponse>
    {
        TRequest Request { get; set; }
        void SetResponse(TResponse response);
    }
}
