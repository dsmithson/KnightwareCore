namespace Knightware.Threading.Tasks
{
    public class AsyncListProcessorItemEventArgs<T>
    {
        public T Item { get; private set; }

        public AsyncListProcessorItemEventArgs(T item)
        {
            this.Item = item;
        }
    }
}
