using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knightware.Collections
{
    [TestClass]
    public class CompositeCollectionTests
    {
        [TestMethod]
        public void AddTest()
        {
            const int expectedChangeCount = 5;
            int actualChangeCount = 0;

            var compositeCollection = new CompositeCollection();
            compositeCollection.CollectionChanged += (sender, e) => actualChangeCount++;

            PopulateCollection(compositeCollection, expectedChangeCount);
            Assert.HasCount(expectedChangeCount, compositeCollection.Collections, "Collections do not appear to have been added");
            Assert.AreEqual(expectedChangeCount, actualChangeCount, "CollectionChanged event count is incorrect");
        }

        [TestMethod]
        public void RemoveTest()
        {
            const int expectedChangeCount = 5;
            int actualChangeCount = 0;

            var compositeCollection = new CompositeCollection();
            PopulateCollection(compositeCollection);

            compositeCollection.CollectionChanged += (sender, e) => actualChangeCount++;
            while (compositeCollection.Collections.Count > 0)
            {
                compositeCollection.Remove(compositeCollection.Collections[0]);
            }
            Assert.AreEqual(expectedChangeCount, actualChangeCount, "Failed to be notified for all collections removed");
        }

        [TestMethod]
        public void RemoveTest2()
        {
            var compositeCollection = new CompositeCollection();
            PopulateCollection(compositeCollection);

            var lists = new List<ObservableCollection<string>>();
            foreach (ObservableCollection<string> list in compositeCollection.Collections)
                lists.Add(list);

            //Hook for a change count after clearing the list
            while (compositeCollection.Collections.Count > 0)
            {
                compositeCollection.Collections.RemoveAt(0);
            }
            int changeCount = 0;
            compositeCollection.CollectionChanged += (s, e) => changeCount++;

            //Now add to the collections and ensure that the list isn't still hooked to collection changed
            foreach (ObservableCollection<string> list in lists)
                list.Add("Test");

            Assert.AreEqual(0, changeCount, "Collection is still hooked to the change notifications of it's previous lists");
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            const int expectedChangeCount = 5;
            int actualChangeCount = 0;

            var compositeCollection = new CompositeCollection();
            PopulateCollection(compositeCollection);

            compositeCollection.CollectionChanged += (sender, e) => actualChangeCount++;
            while (compositeCollection.Collections.Count > 0)
            {
                compositeCollection.RemoveAt(0);
            }
            Assert.AreEqual(expectedChangeCount, actualChangeCount, "Failed to be notified for all collections removed");
        }

        [TestMethod]
        public void ClearTest()
        {
            var compositeCollection = new CompositeCollection();
            PopulateCollection(compositeCollection);

            var lists = new List<ObservableCollection<string>>();
            foreach (ObservableCollection<string> list in compositeCollection.Collections)
                lists.Add(list);

            compositeCollection.Clear();
            Assert.HasCount(0, compositeCollection.Collections, "Failed to clear internal collections (1)");

            int changeCount = 0;
            compositeCollection.CollectionChanged += (s, e) => changeCount++;
            foreach (ObservableCollection<string> list in lists)
            {
                list.Add("Test");
            }
            Assert.AreEqual(0, changeCount, "Collection is still hooked to the change notifications of it's previous lists");
        }

        [TestMethod]
        public void ChildCollectionChangePropagatesToComposite()
        {
            var compositeCollection = new CompositeCollection();
            var childCollection = new ObservableCollection<string> { "Item1" };
            compositeCollection.Add(childCollection);

            int changeCount = 0;
            compositeCollection.CollectionChanged += (s, e) => changeCount++;

            childCollection.Add("Item2");
            Assert.AreEqual(1, changeCount, "Child collection change should propagate to composite");

            childCollection.Remove("Item1");
            Assert.AreEqual(2, changeCount, "Child collection removal should propagate to composite");
        }

        [TestMethod]
        public void NonObservableCollectionCanBeAdded()
        {
            var compositeCollection = new CompositeCollection();
            var nonObservableList = new List<string> { "Item1", "Item2" };

            compositeCollection.Add(nonObservableList);

            Assert.HasCount(1, compositeCollection.Collections);
            Assert.HasCount(2, compositeCollection);
        }

        [TestMethod]
        public void MixedObservableAndNonObservableCollections()
        {
            var compositeCollection = new CompositeCollection();
            var observableList = new ObservableCollection<string> { "Observable1" };
            var nonObservableList = new List<string> { "NonObservable1" };

            compositeCollection.Add(observableList);
            compositeCollection.Add(nonObservableList);

            int changeCount = 0;
            compositeCollection.CollectionChanged += (s, e) => changeCount++;

            // Only observable collection changes should trigger events
            observableList.Add("Observable2");
            Assert.AreEqual(1, changeCount, "Observable collection change should propagate");

            // Non-observable changes won't trigger (as expected behavior)
            nonObservableList.Add("NonObservable2");
            Assert.AreEqual(1, changeCount, "Non-observable collection change should not propagate");
        }

        private static void PopulateCollection(CompositeCollection compositeCollection, int count = 5)
        {
            for (int i = 0; i < count; i++)
            {
                var collection = new ObservableCollection<string> { "Test 1", "Test 2", "Test 3" };
                compositeCollection.Add(collection);
            }
        }
    }
}
