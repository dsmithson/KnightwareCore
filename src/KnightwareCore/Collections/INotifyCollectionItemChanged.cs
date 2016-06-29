using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Knightware.Collections
{
    public delegate void NotifyCollectionItemChangedHandler(object sender, NotifyCollectionItemChangedEventArgs e);

    public interface INotifyCollectionItemChanged : INotifyCollectionChanged
    {
        event NotifyCollectionItemChangedHandler CollectionItemChanged;
    }
}
