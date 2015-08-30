﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Knightware.Collections
{
    public static class ListExtensions
    {
        public static void CopyTo<T, U, K>(this IEnumerable<T> source, IList<U> destination, Func<T, K> getKey, Func<T, U> createNew, Action<T, U> copyFrom)
            where U : T
        {
            CopyTo(source, destination, (item) => getKey(item), (item) => getKey(item), createNew, copyFrom);
        }

        public static void CopyTo<T, U, K>(this IEnumerable<T> source, IList<U> destination, Func<T, K> getSourceKey, Func<U, K> getDestKey, Func<T, U> createNew, Action<T, U> copyFrom)
        {
            ConstrainedCopyTo(source, destination, null, getSourceKey, getDestKey, createNew, copyFrom);
        }

        /// <summary>
        /// Synchronizes a destination list of items from a provided source list, constrained by the type of object
        /// </summary>
        public static void ConstrainedCopyTo<T, U, K, C>(this IEnumerable<T> source, IList<U> destination, Func<T, K> getSourceKey, Func<C, K> getDestKey, Func<T, C> createNew, Action<T, C> copyFrom)
            where C: U
        {
            ConstrainedCopyTo<T,U,K>(
                source,
                destination,
                (item) => item is C,
                getSourceKey,
                (item) => getDestKey((C)item),
                (item) => (U)createNew(item),
                (from, to) => copyFrom(from, (C)to));
        }

        public static void ConstrainedCopyTo<TKey, USrc, UDst>(this IDictionary<TKey, USrc> source, IDictionary<TKey, UDst> destination, Func<UDst, bool> destinationItemsToCompareFilter, Func<USrc, UDst> createNew, Action<USrc, UDst> copyFrom)
        {
            if(source == null || destination == null)
                return;

            //By default, if no dest list filter is provided, all items will be compared
            if (destinationItemsToCompareFilter == null)
                destinationItemsToCompareFilter = new Func<UDst, bool>(item => true);
            
            //Process updates and deletes
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
                    destination.Remove(item.Key);
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
        public static void ConstrainedCopyTo<T, U, K>(this IEnumerable<T> source, IList<U> destination, Func<U, bool> destinationItemsToCompareFilter, Func<T, K> getSourceKey, Func<U, K> getDestKey, Func<T, U> createNew, Action<T, U> copyFrom)
        {
            if (source == null || destination == null || getSourceKey == null || getDestKey == null)
                return;

            //By default, if no dest list filter is provided, all items will be compared
            if(destinationItemsToCompareFilter == null)
                destinationItemsToCompareFilter = new Func<U,bool>(item => true);

            var sourceDict = source.ToDictionary((item) => getSourceKey(item));
            var destDict = destination.Where(destinationItemsToCompareFilter).ToDictionary((item) => getDestKey(item));

            ConstrainedCopyTo(sourceDict, destDict, destinationItemsToCompareFilter, createNew, copyFrom);
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
