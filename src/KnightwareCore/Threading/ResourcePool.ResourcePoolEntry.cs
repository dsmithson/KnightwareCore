using Knightware.Threading.Tasks;
using System;
using System.Threading.Tasks;

namespace Knightware.Threading
{
    public partial class ResourcePool<T>
    {
        private class ResourcePoolEntry
        {
            private TaskCompletionSource<bool> inUseCompletionSource;
            private readonly AsyncLock inUseCompletionSourceLock = new AsyncLock();

            public T Connection { get; private set; }
            public string SerializationKey { get; private set; }
            public DateTime LastUseTime { get; private set; }
            public bool InUse { get; private set; }

            public ResourcePoolEntry(T connection)
            {
                this.Connection = connection;
            }

            public bool Acquire(string serializationKey)
            {
                if (InUse)
                    return false;

                InUse = true;
                LastUseTime = DateTime.Now;
                SerializationKey = serializationKey;
                return true;
            }

            public async Task ReleaseAsync()
            {
                using (var lockObject = await inUseCompletionSourceLock.LockAsync())
                {
                    if (inUseCompletionSource != null)
                    {
                        inUseCompletionSource.TrySetResult(true);
                        inUseCompletionSource = null;
                    }
                }
                InUse = false;
                LastUseTime = DateTime.Now;
                SerializationKey = null;
            }

            public async Task WaitForRelease()
            {
                Task task = null;
                using (var lockObject = await inUseCompletionSourceLock.LockAsync())
                {
                    if (inUseCompletionSource == null)
                    {
                        inUseCompletionSource = new TaskCompletionSource<bool>();
                    }
                    task = inUseCompletionSource.Task;
                }
                await task;
            }
        }
    }
}
