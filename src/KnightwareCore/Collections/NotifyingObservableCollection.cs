using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Knightware.Collections
{
    /// <summary>
    /// Extends the generic ObservableCollection by raising a property changed event for any item in the collection implementing the INotifyPropertyChanged interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NotifyingObservableCollection<T> : ObservableCollection<T>
    {
        public event NotifyCollectionItemChangedHandler CollectionItemChanged;
        protected virtual void OnCollectionItemChanged(NotifyCollectionItemChangedEventArgs e)
        {
            if (CollectionItemChanged != null)
            {
                using (BlockReentrancy())
                {
                    CollectionItemChanged(this, e);
                }
            }
        }

        public NotifyingObservableCollection()
        {
        }

        public NotifyingObservableCollection(IEnumerable<T> items)
        {
            if (items == null)
                return;

            foreach (T item in items)
                this.Add(item);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                //Add INotifyPropertyChanged handler
                foreach (T item in e.NewItems)
                    hookPropertyChange(item);
            }
            if (e.OldItems != null && e.OldItems.Count > 0)
            {
                //Remove INotifyPropertyChanged handler
                foreach (T item in e.OldItems)
                    unhookPropertyChange(item);
            }
        }

        protected override void ClearItems()
        {
            foreach (T item in Items)
                unhookPropertyChange(item);

            base.ClearItems();
        }

        void NotifyingObservableCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnCollectionItemChanged(new NotifyCollectionItemChangedEventArgs(sender, e.PropertyName, this.IndexOf((T)sender)));
        }

        private void hookPropertyChange(T item)
        {
            var propertyChangedItem = item as INotifyPropertyChanged;
            if (propertyChangedItem != null)
                propertyChangedItem.PropertyChanged += new PropertyChangedEventHandler(NotifyingObservableCollection_PropertyChanged);
        }

        private void unhookPropertyChange(T item)
        {
            var propertyChangedItem = item as INotifyPropertyChanged;
            if (propertyChangedItem != null)
                propertyChangedItem.PropertyChanged -= new PropertyChangedEventHandler(NotifyingObservableCollection_PropertyChanged);
        }
    }
}
