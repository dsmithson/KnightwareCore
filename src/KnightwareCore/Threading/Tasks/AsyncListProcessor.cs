using Knightware.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Knightware.Threading.Tasks
{
    /// <summary>
    /// Manages a collection of items which can be added to in a thread-safe mannor, and will be processed sequentially
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncListProcessor<T> : IDisposable
    {
        private ActionBlock<T> workerBlock;
        private CancellationTokenSource cancellationTokenSource;
        private readonly Func<AsyncListProcessorItemEventArgs<T>, Task> processItem;
        private readonly Func<bool> checkForContinueMethod;
        private bool disposed;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Number of threads that will be used to process items
        /// </summary>
        public int MaxDegreeOfParallelism { get; private set; }

        /// <summary>
        /// Maximimum items to allow to be in the queue, or 0 for no limit.
        /// </summary>
        public int MaximumQueueCount { get; private set; }

        public AsyncListProcessor(Func<AsyncListProcessorItemEventArgs<T>, Task> processItem, Func<bool> checkForContinueMethod = null, int maxDegreeOfParallelism = 1, int maxQueueCount = 0)
        {
            this.processItem = processItem ?? throw new ArgumentException("ProcessItem may not be null", "processItem");
            this.checkForContinueMethod = checkForContinueMethod;
            this.MaxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        public async Task<bool> StartupAsync()
        {
            await ShutdownAsync();
            IsRunning = true;

            this.cancellationTokenSource = new CancellationTokenSource();

            var workerBlockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationTokenSource.Token
            };
            if (MaximumQueueCount > 0)
            {
                workerBlockOptions.BoundedCapacity = MaximumQueueCount;
            }

            this.workerBlock = new ActionBlock<T>(
                async (item) =>
                {
                    try
                    {
                        //Check to see that we should still be running
                        if (IsRunning && (checkForContinueMethod == null || checkForContinueMethod()) && !workerBlockOptions.CancellationToken.IsCancellationRequested)
                            await processItem(new AsyncListProcessorItemEventArgs<T>(item));
                    }
                    catch (Exception ex)
                    {
                        TraceQueue.Trace(this, TracingLevel.Warning, "{0} occurred while processing item: {1}", ex.GetType().Name, ex.Message);
                    }
                },
                workerBlockOptions);

            return true;
        }

        /// <summary>
        /// Shuts down the async list processor.
        /// </summary>
        /// <param name="maxWait">Amount of time, in milliseconds, to wait for a current running task to complete.</param>
        /// <returns>True if shutdown was successful, or false if maxWait elapsed.  A return of false indicates the worker task has not yet completed.</returns>
        public async Task<bool> ShutdownAsync(int maxWait = System.Threading.Timeout.Infinite)
        {
            IsRunning = false;

            try
            {
                if (workerBlock != null)
                {
                    workerBlock.Complete();
                    cancellationTokenSource.Cancel();
                    await workerBlock.Completion;

                    workerBlock = null;
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
                return true;
            }
            catch (TaskCanceledException)
            {
                workerBlock = null;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                return true;
            }
            catch (Exception ex)
            {
                TraceQueue.Trace(this, TracingLevel.Error, "{0} occurred while shutting down AsyncListProcessor: {1}", ex.GetType().Name, ex.Message);
                return false;
            }
        }

        public void Add(T newItem)
        {
            if (newItem == null || !IsRunning || workerBlock == null)
                return;

            workerBlock.Post(newItem);
        }

        public void AddRange(IEnumerable<T> newItems)
        {
            if (newItems != null)
            {
                foreach (T newItem in newItems)
                    Add(newItem);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                IsRunning = false;
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                workerBlock = null;
            }
        }
    }
}
