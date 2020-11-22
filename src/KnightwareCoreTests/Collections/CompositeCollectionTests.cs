using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            Assert.AreEqual(expectedChangeCount, compositeCollection.Collections.Count, "Collections do not appear to have been added");
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
            Assert.AreEqual(0, compositeCollection.Collections.Count, "Failed to clear internal collections (1)");

            int changeCount = 0;
            compositeCollection.CollectionChanged += (s, e) => changeCount++;
            foreach (ObservableCollection<string> list in lists)
            {
                list.Add("Test");
            }
            Assert.AreEqual(0, changeCount, "Collection is still hooked to the change notifications of it's previous lists");
        }

        private void PopulateCollection(CompositeCollection compositeCollection, int count = 5)
        {
            for (int i = 0; i < count; i++)
            {
                var collection = new ObservableCollection<string> { "Test 1", "Test 2", "Test 3" };
                compositeCollection.Add(collection);
            }
        }
    }
}
