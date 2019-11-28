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
            resourcePoolMonitor = new AutoResetWorker()
            {
                PeriodicSignallingTime = TimeSpan.FromSeconds(1) //May need to re-evaluate this interval
            };
            if (!await resourcePoolMonitor.StartupAsync(connectionPoolMonitor_DoWork, null, () => IsRunning))
            {
                await ShutdownAsync();
                return false;
            }
            resourcePoolInitializing = true;
            resourcePoolMonitor.Set();

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
                await resourcePoolMonitor.ShutdownAsync();
                resourcePoolMonitor = null;
            }

            //Close any lingering pool connections   
            DateTime timeoutTime = maxWait >= 0 ? DateTime.MaxValue : DateTime.Now.AddMilliseconds(maxWait);
            while (resourcePool.Count > 0)
            {
                var shutdownTasks = new List<Task>();
                bool timeoutExpired = DateTime.Now < timeoutTime;
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

                if (shutdownTasks.Count > 0)
                    await Task.WhenAll(shutdownTasks.ToArray());
                else
                    await Task.Delay(10);
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
            using (var lockObject = await requestQueueLock.LockAsync())
            {
                requestQueue.Add(request);
            }
            resourcePoolMonitor.Set();
            return await request.Task;
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
            using (var lockObject = await resourcePoolLock.LockAsync())
            {
                ResourcePoolEntry entry = resourcePool.FirstOrDefault(c => object.Equals(c.Connection, acquiredConnection));
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
                    resourcePoolMonitor.Set();
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

        /// <summary>
        /// Acquires a resource from the pool and passes it to a specified function to be run
        /// </summary>
        public async Task<TResponse> Run<TResponse>(Func<T, Task<TResponse>> actionToRun, string serializationKey = null)
        {
            var resource = await Acquire(serializationKey);
            TResponse result = default(TResponse);
            try
            {
                result = await actionToRun(resource);
                await Release(resource, false);
            }
            catch
            {
                await Release(resource, true);
                throw;
            }

            return result;
        }


        private DateTime lastResourceCreationCheck;
        private DateTime lastResourceClosedCheck;
        private async Task connectionPoolMonitor_DoWork(object state)
        {
            DateTime now = DateTime.Now;

            //Create a new connection if needed
            bool newConnectionsAdded = false;
            if (resourcePoolInitializing || (requestQueue.Count > 0 && resourcePool.Count < MaximumConnections && now.Subtract(lastResourceCreationCheck) > ResourceAllocationInterval))
            {
                newConnectionsAdded = true;
                T connection = await createResourceHandler();

                //If we don't have a connection, we'll retry automatically when our auto-elapsed timer expires or someone needs a connection
                if (connection != null)
                {
                    //Create a new connection pool state object to the pool
                    resourcePool.Add(new ResourcePoolEntry(connection));

                    //If we're not done initializing the pool, trigger this worker to run again immediately
                    if (resourcePoolInitializing)
                    {
                        if (resourcePool.Count >= InitialConnections)
                            resourcePoolInitializing = false;
                        else
                            resourcePoolMonitor.Set();
                    }
                }

                lastResourceCreationCheck = DateTime.Now;
            }

            //Try to allocate pending requests into the connection pool
            using (var requestLock = await requestQueueLock.LockAsync())
            using (var poolLock = await resourcePoolLock.LockAsync())
            {
                var availableConnections = resourcePool.Where(c => !c.InUse).ToList();

                int index = 0;
                while (index < requestQueue.Count && availableConnections.Count > 0)
                {
                    var request = requestQueue[index];

                    //Check to see if there is something already running with this serialization key
                    //in which case we can't yet process this request
                    if (!string.IsNullOrEmpty(request.SerializationKey) &&
                        resourcePool.Any(c => c.SerializationKey == request.SerializationKey))
                    {
                        index++;
                        continue;
                    }

                    //Grab the next available allocation
                    var availableConnection = availableConnections[0];
                    availableConnections.RemoveAt(0);

                    //Assign the connection to a request
                    availableConnection.Acquire(request.SerializationKey);
                    request.Tcs.TrySetResult(availableConnection.Connection);
                }
            }

            //Check to see if we need to remove pending requests
            if (!newConnectionsAdded
                && requestQueue.Count == 0 && now.Subtract(lastResourceClosedCheck) > ResourceDeallocationInterval
                && resourcePool.Count > MinimumConnections)
            {
                using (var poolLock = await resourcePoolLock.LockAsync())
                {
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
    }
}
