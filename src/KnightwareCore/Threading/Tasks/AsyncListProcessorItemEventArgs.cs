using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
