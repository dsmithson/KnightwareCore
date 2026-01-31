using Knightware.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Knightware.Threading
{
    public partial class ResourcePool<T>
    {
        private Func<T, Task> closeResourceHandler;
        private Func<Task<T>> createResourceHandler;

        private List<ResourcePoolEntry> resourcePool;
        private readonly AsyncLock resourcePoolLock = new AsyncLock();

        private AutoResetWorker resourcePoolMonitor;
        private bool resourcePoolInitializing;
        private List<Request> requestQueue;
        private readonly AsyncLock requestQueueLock = new AsyncLock();

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Number of connections to be created upon startup
        /// </summary>
        public int InitialConnections { get; set; } = 5;

        /// <summary>
        /// Number of connections to be maintained, regardless of pool demand
        /// </summary>
        public int MinimumConnections { get; set; } = 1;

        /// <summary>
        /// Maximum number of connections to create, regardless of pool demand
        /// </summary>
        public int MaximumConnections { get; set; } = 10;

        /// <summary>
        /// Amount of time before a new resource will be created when the pending requests remains > 1
        /// </summary>
        public TimeSpan ResourceAllocationInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Amount of time before an existing resource will be closed when the pool remains underutilized
        /// </summary>
        public TimeSpan ResourceDeallocationInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Startup the resource pool
        /// </summary>
        public async Task<bool> StartupAsync(Func<Task<T>> createConnectionHandler, Func<T, Task> closeConnectionHandler)
        {
            await ShutdownAsync();
            IsRunning = true;

            this.createResourceHandler = createConnectionHandler;
            this.closeResourceHandler = closeConnectionHandler;

            requestQueue = new List<Request>();
            resourcePool = new List<ResourcePoolEntry>();

            //Initialize the worker which will increase/decrease the pool size as necessary
            const double minSignallingTimeMs = 100;
            resourcePoolMonitor = new AutoResetWorker()
            {
                PeriodicSignallingTime = TimeSpan.FromMilliseconds(
                    Math.Max(minSignallingTimeMs, 
                    Math.Min(ResourceAllocationInterval.TotalMilliseconds, ResourceDeallocationInterval.TotalMilliseconds)))
            };
            if (!await resourcePoolMonitor.StartupAsync(connectionPoolMonitor_DoWork, null, () => IsRunning).ConfigureAwait(false))
            {
                await ShutdownAsync().ConfigureAwait(false);
                return false;
            }
            resourcePoolInitializing = true;
            resourcePoolMonitor.Set();

            //Wait for initial connections to be created
            while (resourcePoolInitializing && IsRunning)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }

            return true;
        }

        /// <summary>
        /// Shutdown resource pool
        /// </summary>
        public async Task ShutdownAsync(int maxWait = -1)
        {
            IsRunning = false;

            if (resourcePoolMonitor != null)
            {
                await resourcePoolMonitor.ShutdownAsync().ConfigureAwait(false);
                resourcePoolMonitor = null;
            }

            // Cancel any pending requests that never got a resource
            if (requestQueue != null)
            {
                using (var requestLock = await requestQueueLock.LockAsync().ConfigureAwait(false))
                {
                    foreach (var request in requestQueue)
                    {
                        request.Tcs.TrySetResult(default(T));
                    }
                    requestQueue.Clear();
                }
            }

            //Close any lingering pool connections   
            DateTime timeoutTime = maxWait >= 0 ? DateTime.Now.AddMilliseconds(maxWait) : DateTime.MaxValue;
            while (resourcePool?.Count > 0)
            {
                var shutdownTasks = new List<Task>();
                bool timeoutExpired = DateTime.Now > timeoutTime;
                
                using (var poolLock = await resourcePoolLock.LockAsync().ConfigureAwait(false))
                {
                    int index = 0;
                    while (index < resourcePool.Count)
                    {
                        var resource = resourcePool[index];
                        if (resource.InUse && !timeoutExpired)
                        {
                            //Give in-use resources time to finish their work before we close their connection
                            index++;
                        }
                        else
                        {
                            resourcePool.RemoveAt(index);
                            shutdownTasks.Add(closeResourceHandler(resource.Connection));
                        }
                    }
                }

                if (shutdownTasks.Count > 0)
                    await Task.WhenAll(shutdownTasks).ConfigureAwait(false);
                
                // If no tasks were removed, release lock and wait for Release calls to complete
                if (resourcePool?.Count > 0)
                    await Task.Delay(50).ConfigureAwait(false);
            }

            createResourceHandler = null;
            closeResourceHandler = null;
            requestQueue = null;
            resourcePool = null;
        }

        /// <summary>
        /// Acquires a connection from the pool
        /// </summary>
        /// <param name="serializationKey">Optional value which will serialize access to the pool.</param>
        /// <returns></returns>
        public async Task<T> Acquire(string serializationKey = null)
        {
            if (!IsRunning)
                return default(T);

            //Enqueue the pending request and trigger the connection pool worker to get it scheduled
            var request = new Request(serializationKey);
            using (var lockObject = await requestQueueLock.LockAsync().ConfigureAwait(false))
            {
                requestQueue.Add(request);
            }
            resourcePoolMonitor.Set();
            return await request.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Releases a previously acquired resource (using the Acquire method) back to the pool
        /// </summary>
        /// <param name="acquiredConnection">Pooled resource to return back to the pool</param>
        /// <param name="resourceShouldBeShutdown">If the resource is found to be fouled, or should not be used again, set this to true to remove it from the pool.</param>
        /// <returns>True if returned to pool successfully, or false if the resource was not found</returns>
        public async Task<bool> Release(T acquiredConnection, bool resourceShouldBeShutdown = false)
        {
            //Release the connection to the pool and trigger the pool to re-evaluate pending connection requests
            using (var lockObject = await resourcePoolLock.LockAsync().ConfigureAwait(false))
            {
                ResourcePoolEntry entry = resourcePool?.FirstOrDefault(c => object.Equals(c.Connection, acquiredConnection));
                if (entry != null)
                {
                    if (resourceShouldBeShutdown)
                    {
                        resourcePool.Remove(entry);
                        Task t = closeResourceHandler(entry.Connection);
                    }
                    else
                    {
                        //Return back to pool
                        await entry.ReleaseAsync();
                    }
                    resourcePoolMonitor?.Set();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Acquires a resource from the pool and passes it to a specified function to be run
        /// </summary>
        public Task Run(Func<T, Task> actionToRun, string serializationKey = null)
        {
            return Run(async (t) =>
            {
                await actionToRun(t);
                return true;
            }, serializationKey);
        }

        public Task<TResponse> RunOnThreadPool<TResponse>(Func<T, Task<TResponse>> actionToRun, string serializationKey = null)
        {
            return Task.Run(() => Run(actionToRun, serializationKey));
        }

        /// <summary>
        /// Acquires a resource from the pool and passes it to a specified function to be run
        /// </summary>
        public async Task<TResponse> Run<TResponse>(Func<T, Task<TResponse>> actionToRun, string serializationKey = null)
        {
            var resource = await Acquire(serializationKey).ConfigureAwait(false);
            
            // If pool is shutting down, Acquire returns default(T) - don't run the action
            if (!IsRunning)
                return default;
                
            TResponse result;
            try
            {
                result = await actionToRun(resource).ConfigureAwait(false);

                await Release(resource, false).ConfigureAwait(false);
            }
            catch
            {
                await Release(resource, true).ConfigureAwait(false);
                throw;
            }

            return result;
        }





        private DateTime lastResourceCreationCheck;
        private DateTime lastResourceClosedCheck;

        private async Task connectionPoolMonitor_DoWork(object state)
        {
            DateTime now = DateTime.Now;

            await TryCreateResourceAsync(now).ConfigureAwait(false);
            await AllocateRequestsToAvailableResourcesAsync(now).ConfigureAwait(false);
        }

        private async Task TryCreateResourceAsync(DateTime now)
        {
            bool shouldCreate = await ShouldCreateResourceAsync(now).ConfigureAwait(false);
            if (!shouldCreate)
                return;

            T connection = await createResourceHandler().ConfigureAwait(false);

            using (await resourcePoolLock.LockAsync().ConfigureAwait(false))
            {
                resourcePool.Add(new ResourcePoolEntry(connection));

                if (resourcePoolInitializing && resourcePool.Count >= InitialConnections)
                {
                    resourcePoolInitializing = false;
                }
                else if (resourcePoolInitializing)
                {
                    resourcePoolMonitor.Set();
                }
            }

            lastResourceCreationCheck = DateTime.Now;
        }

        private async Task<bool> ShouldCreateResourceAsync(DateTime now)
        {
            if (resourcePoolInitializing)
                return true;

            int requestCount;
            int poolCount;
            int availableCount;

            using (await requestQueueLock.LockAsync().ConfigureAwait(false))
            using (await resourcePoolLock.LockAsync().ConfigureAwait(false))
            {
                requestCount = requestQueue.Count;
                poolCount = resourcePool.Count;
                availableCount = resourcePool.Count(c => !c.InUse);
            }

            if (poolCount >= MaximumConnections)
                return false;

            bool urgentNeed = requestCount > 0 && availableCount == 0;
            if (urgentNeed)
                return true;

            bool needsMoreResources = requestCount > availableCount;
            bool timeIntervalPassed = now.Subtract(lastResourceCreationCheck) > ResourceAllocationInterval;
            return needsMoreResources && timeIntervalPassed;
        }

        private async Task AllocateRequestsToAvailableResourcesAsync(DateTime now)
        {
            using (await requestQueueLock.LockAsync().ConfigureAwait(false))
            using (await resourcePoolLock.LockAsync().ConfigureAwait(false))
            {
                AssignResourcesToRequests();
                TriggerMoreAllocationsIfNeeded();
                TryDeallocateStaleResource(now);
            }
        }

        private void AssignResourcesToRequests()
        {
            var availableConnections = resourcePool.Where(c => !c.InUse).ToList();

            int index = 0;
            while (index < requestQueue.Count && availableConnections.Count > 0)
            {
                var request = requestQueue[index];

                if (IsRequestBlockedBySerialization(request))
                {
                    index++;
                    continue;
                }

                var connection = availableConnections[0];
                availableConnections.RemoveAt(0);

                requestQueue.RemoveAt(index);
                connection.Acquire(request.SerializationKey);
                request.Tcs.TrySetResult(connection.Connection);
            }
        }

        private bool IsRequestBlockedBySerialization(Request request)
        {
            return !string.IsNullOrEmpty(request.SerializationKey) &&
                   resourcePool.Any(c => c.SerializationKey == request.SerializationKey);
        }

        private void TriggerMoreAllocationsIfNeeded()
        {
            if (requestQueue.Count > 0 && resourcePool.Count < MaximumConnections)
            {
                resourcePoolMonitor.Set();
            }
        }

        private void TryDeallocateStaleResource(DateTime now)
        {
            if (resourcePoolInitializing || requestQueue.Count > 0)
                return;

            if (now.Subtract(lastResourceClosedCheck) <= ResourceDeallocationInterval)
                return;

            if (resourcePool.Count <= MinimumConnections)
                return;

            var staleConnection = resourcePool
                .Where(c => !c.InUse && now.Subtract(c.LastUseTime) > ResourceDeallocationInterval)
                .OrderBy(c => c.LastUseTime)
                .FirstOrDefault();

            if (staleConnection != null)
            {
                resourcePool.Remove(staleConnection);
                Task t = closeResourceHandler(staleConnection.Connection);
            }

            lastResourceClosedCheck = now;
        }
    }
}
