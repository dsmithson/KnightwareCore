using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Collections
{
    public static class ListExtensions
    {
        public static void CopyTo<T, U, K>(this IEnumerable<T> source, IList<U> destination, Func<T, K> getKey, Func<T, U> createNew, Action<T, U> copyFrom, Action<U> onRemoved = null)
            where U : T
        {
            CopyTo(source, destination, (item) => getKey(item), (item) => getKey(item), createNew, copyFrom, onRemoved);
        }

        public static void CopyTo<T, U, K>(this IEnumerable<T> source, IList<U> destination, Func<T, K> getSourceKey, Func<U, K> getDestKey, Func<T, U> createNew, Action<T, U> copyFrom, Action<U> onRemoved = null)
        {
            ConstrainedCopyTo(source, destination, null, getSourceKey, getDestKey, createNew, copyFrom, onRemoved);
        }

        /// <summary>
        /// Synchronizes a destination list of items from a provided source list, constrained by the type of object
        /// </summary>
        public static void ConstrainedCopyTo<T, U, K, C>(this IEnumerable<T> source, IList<U> destination, Func<T, K> getSourceKey, Func<C, K> getDestKey, Func<T, C> createNew, Action<T, C> copyFrom, Action<U> onRemoved = null)
            where C: U
        {
            ConstrainedCopyTo<T,U,K>(
                source,
                destination,
                (item) => item is C,
                getSourceKey,
                (item) => getDestKey((C)item),
                (item) => (U)createNew(item),
                (from, to) => copyFrom(from, (C)to),
                (removed) =>
                {
                    if (onRemoved != null)
                        onRemoved(removed);
                });
        }

        public static void ConstrainedCopyTo<TKey, USrc, UDst>(this IDictionary<TKey, USrc> source, IDictionary<TKey, UDst> destination, Func<UDst, bool> destinationItemsToCompareFilter, Func<USrc, UDst> createNew, Action<USrc, UDst> copyFrom, Action<UDst> onRemoved = null)
        {
            if(source == null || destination == null)
                return;

            //By default, if no dest list filter is provided, all items will be compared
            if (destinationItemsToCompareFilter == null)
                destinationItemsToCompareFilter = new Func<UDst, bool>(item => true);
            
            //Process updates and deletes
            var removeList = new List<KeyValuePair<TKey, UDst>>();
            foreach (var item in destination)
            {
                if (source.ContainsKey(item.Key))
                {
                    //Update the existing item in the destination list
                    copyFrom(source[item.Key], item.Value);
                }
                else
                {
                    //Item doesn't exist in the source list - remove it
                    removeList.Add(item);
                }
            }

            if (removeList.Count > 0)
            {
                foreach (var item in removeList)
                {
                    destination.Remove(item.Key);
                    if(onRemoved != null)
                        onRemoved(item.Value);
                }
            }

            //Process additions
            foreach (var item in source.Where(existing => !destination.ContainsKey(existing.Key)))
            {
                UDst newItem = createNew(item.Value);
                copyFrom(item.Value, newItem);
                destination.Add(item.Key, newItem);
            }
        }

        /// <summary>
        /// Synchronizes a source list with a destination list, but limits the comparison and removal of destination list items to a provided filtered subset of items
        /// </summary>
        public static void ConstrainedCopyTo<T, U, K>(this IEnumerable<T> source, IList<U> destination, Func<U, bool> destinationItemsToCompareFilter, Func<T, K> getSourceKey, Func<U, K> getDestKey, Func<T, U> createNew, Action<T, U> copyFrom, Action<U> onRemoved = null)
        {
            if (source == null || destination == null || getSourceKey == null || getDestKey == null)
                return;

            //By default, if no dest list filter is provided, all items will be compared
            if(destinationItemsToCompareFilter == null)
                destinationItemsToCompareFilter = new Func<U,bool>(item => true);

            var sourceDict = source.ToDictionary((item) => getSourceKey(item));
            var destDict = destination.Where(destinationItemsToCompareFilter).ToDictionary((item) => getDestKey(item));
            
            //Process updates and deletes
            var removeList = new Dictionary<K, U>();
            foreach (var destItem in destDict)
            {
                if (sourceDict.ContainsKey(destItem.Key))
                {
                    //Update the existing item in the destination list
                    copyFrom(sourceDict[destItem.Key], destItem.Value);
                }
                else
                {
                    //Item doesn't exist in the source list - remove it
                    removeList.Add(destItem.Key, destItem.Value);
                }
            }

            foreach (var removeItem in removeList)
            {
                destDict.Remove(removeItem.Key);
                destination.Remove(removeItem.Value);

                if(onRemoved != null)
                    onRemoved(removeItem.Value);
            }

            //Process additions
            foreach (var sourceItem in sourceDict.Where(existing => !destDict.ContainsKey(existing.Key)))
            {
                U newItem = createNew(sourceItem.Value);
                copyFrom(sourceItem.Value, newItem);
                destination.Add(newItem);
            }
        }

        public static void RemoveWhere<K, V>(this IDictionary<K, V> source, Func<V, bool> removeIf)
        {
            List<K> keysToRemove = new List<K>();

            foreach(var item in source)
            {
                if (removeIf(item.Value))
                    keysToRemove.Add(item.Key);                    
            }

            foreach (K key in keysToRemove)
                source.Remove(key);
        }
    }
}
