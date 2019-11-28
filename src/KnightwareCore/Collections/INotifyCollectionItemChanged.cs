using System.Collections.Specialized;

namespace Knightware.Collections
{
    public delegate void NotifyCollectionItemChangedHandler(object sender, NotifyCollectionItemChangedEventArgs e);

    public interface INotifyCollectionItemChanged : INotifyCollectionChanged
    {
        event NotifyCollectionItemChangedHandler CollectionItemChanged;
    }
}
