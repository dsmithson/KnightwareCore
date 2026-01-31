using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Knightware.Collections
{
    public class CompositeCollection : ICollection, INotifyCollectionChanged
    {
        private readonly object syncRoot = new object();
        private readonly ObservableCollection<IList> collections = new ObservableCollection<IList>();
        private readonly List<INotifyCollectionChanged> registeredNotifyingCollections = new List<INotifyCollectionChanged>();
        public ObservableCollection<IList> Collections
        {
            get { return collections; }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }

        public CompositeCollection()
        {
            collections.CollectionChanged += collections_CollectionChanged;
        }

        public void Add(IList collection)
        {
            Remove(collection);
            collections.Add(collection);
        }

        public void RemoveAt(int index)
        {
            collections.RemoveAt(index);
        }

        public void Remove(IList collection)
        {
            if (collections.Contains(collection))
                collections.Remove(collection);
        }

        public void Clear()
        {
            collections.Clear();
        }

        void collections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                UnregisterAllCollections();
            }

            UnregisterRemovedCollections(e.OldItems);
            RegisterNewCollections(e.NewItems);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void UnregisterAllCollections()
        {
            foreach (var notifyingCollection in registeredNotifyingCollections)
            {
                notifyingCollection.CollectionChanged -= observable_CollectionChanged;
            }
            registeredNotifyingCollections.Clear();
        }

        private void UnregisterRemovedCollections(IList oldItems)
        {
            if (oldItems == null || oldItems.Count == 0)
                return;

            foreach (IList collection in oldItems)
            {
                if (collection is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged -= observable_CollectionChanged;
                    registeredNotifyingCollections.Remove(observable);
                }
            }
        }

        private void RegisterNewCollections(IList newItems)
        {
            if (newItems == null || newItems.Count == 0)
                return;

            foreach (IList collection in newItems)
            {
                if (collection is INotifyCollectionChanged observable)
                {
                    observable.CollectionChanged += observable_CollectionChanged;
                    registeredNotifyingCollections.Add(observable);
                }
            }
        }

        void observable_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void CopyTo(Array array, int index)
        {
            foreach (object item in this)
            {
                array.SetValue(item, index++);
            }
        }

        public int Count
        {
            get { return collections.Sum(collection => collection.Count); }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return syncRoot; }
        }

        public IEnumerator GetEnumerator()
        {
            return new CompositeCollectionEnumerator(collections);
        }

        protected class CompositeCollectionEnumerator : IEnumerator
        {
            private readonly ObservableCollection<IList> collections;
            private IEnumerator currentEnumerator;
            private int currentCollectionIndex;

            public CompositeCollectionEnumerator(ObservableCollection<IList> collections)
            {
                this.collections = collections;
                Reset();
            }

            public object Current
            {
                get { return (currentEnumerator == null ? null : currentEnumerator.Current); }
            }

            public bool MoveNext()
            {
                if (currentEnumerator == null)
                    return false;

                if (currentEnumerator.MoveNext())
                    return true;

                //If we returned false above, lets move to the next collection
                while (++currentCollectionIndex < collections.Count)
                {
                    //Lets move to the next collection, and try to move it to the first position
                    currentEnumerator = collections[currentCollectionIndex].GetEnumerator();
                    if (currentEnumerator.MoveNext())
                        return true;
                }

                //No more collections to iterate
                return false;
            }

            public void Reset()
            {
                currentEnumerator = (collections.Count == 0 ? null : collections[0].GetEnumerator());
            }
        }
    }
}
