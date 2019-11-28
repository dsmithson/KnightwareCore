using System.ComponentModel;

namespace Knightware.Collections
{
    public class NotifyCollectionItemChangedEventArgs : PropertyChangedEventArgs
    {
        public object ObjectChanged { get; set; }
        public int CollectionIndex { get; set; }

        public NotifyCollectionItemChangedEventArgs(object objectChanged, string propertyName, int collectionIndex)
            : base(propertyName)
        {
            this.ObjectChanged = objectChanged;
            this.CollectionIndex = collectionIndex;
        }
    }
}
