using System.Threading.Tasks;

namespace Knightware.Threading
{
    public partial class ResourcePool<T>
    {
        public class Request
        {
            public TaskCompletionSource<T> Tcs { get; } = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            public string SerializationKey { get; private set; }

            public Task<T> Task { get { return Tcs.Task; } }

            public Request(string serializationKey = null)
            {
                this.SerializationKey = serializationKey;
            }
        }
    }
}
