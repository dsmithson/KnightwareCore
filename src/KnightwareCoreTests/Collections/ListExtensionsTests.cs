using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Knightware.Collections
{
    [TestClass]
    public class ListExtensionsTests
    {
        private class SourceItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class DestItem : SourceItem
        {
            public bool WasUpdated { get; set; }
        }

        [TestMethod]
        public void CopyToAddsNewItemsTest()
        {
            var source = new List<SourceItem>
            {
                new SourceItem { Id = 1, Name = "Item1" },
                new SourceItem { Id = 2, Name = "Item2" }
            };
            var destination = new List<DestItem>();

            source.CopyTo<SourceItem, DestItem, int>(
                destination,
                s => s.Id,
                s => new DestItem { Id = s.Id },
                (s, d) => d.Name = s.Name);

            Assert.AreEqual(2, destination.Count);
            Assert.AreEqual("Item1", destination.First(d => d.Id == 1).Name);
            Assert.AreEqual("Item2", destination.First(d => d.Id == 2).Name);
        }

        [TestMethod]
        public void CopyToUpdatesExistingItemsTest()
        {
            var source = new List<SourceItem>
            {
                new SourceItem { Id = 1, Name = "Updated" }
            };
            var destination = new List<DestItem>
            {
                new DestItem { Id = 1, Name = "Original" }
            };

            source.CopyTo<SourceItem, DestItem, int>(
                destination,
                s => s.Id,
                s => new DestItem { Id = s.Id },
                (s, d) => d.Name = s.Name);

            Assert.AreEqual(1, destination.Count);
            Assert.AreEqual("Updated", destination[0].Name);
        }

        [TestMethod]
        public void CopyToRemovesMissingItemsTest()
        {
            var source = new List<SourceItem>
            {
                new SourceItem { Id = 1, Name = "Item1" }
            };
            var destination = new List<DestItem>
            {
                new DestItem { Id = 1, Name = "Item1" },
                new DestItem { Id = 2, Name = "Item2" }
            };

            DestItem removedItem = null;
            source.CopyTo<SourceItem, DestItem, int>(
                destination,
                s => s.Id,
                s => new DestItem { Id = s.Id },
                (s, d) => d.Name = s.Name,
                removed => removedItem = removed);

            Assert.HasCount(1, destination);
            Assert.IsNotNull(removedItem);
            Assert.AreEqual(2, removedItem.Id);
        }

        [TestMethod]
        public void CopyToWithNullSourceDoesNotThrowTest()
        {
            IEnumerable<SourceItem> source = null;
            var destination = new List<DestItem>();

            source.CopyTo<SourceItem, DestItem, int>(
                destination,
                s => s.Id,
                s => new DestItem(),
                (s, d) => { });
        }

        [TestMethod]
        public void CopyToWithNullDestinationDoesNotThrowTest()
        {
            var source = new List<SourceItem> { new SourceItem { Id = 1 } };
            source.CopyTo<SourceItem, DestItem, int>(
                null,
                s => s.Id,
                s => new DestItem(),
                (s, d) => { });
        }

        [TestMethod]
        public void ConstrainedCopyToDictionaryTest()
        {
            var source = new Dictionary<string, SourceItem>
            {
                { "a", new SourceItem { Id = 1, Name = "Item1" } },
                { "b", new SourceItem { Id = 2, Name = "Item2" } }
            };
            var destination = new Dictionary<string, DestItem>();

            source.ConstrainedCopyTo(
                destination,
                null,
                s => new DestItem { Id = s.Id },
                (s, d) => d.Name = s.Name);

            Assert.AreEqual(2, destination.Count);
            Assert.AreEqual("Item1", destination["a"].Name);
        }

        [TestMethod]
        public void ConstrainedCopyToDictionaryRemovesOldItemsTest()
        {
            var source = new Dictionary<string, SourceItem>
            {
                { "a", new SourceItem { Id = 1, Name = "Item1" } }
            };
            var destination = new Dictionary<string, DestItem>
            {
                { "a", new DestItem { Id = 1, Name = "Original" } },
                { "b", new DestItem { Id = 2, Name = "ToBeRemoved" } }
            };

            DestItem removedItem = null;
            source.ConstrainedCopyTo(
                destination,
                null,
                s => new DestItem { Id = s.Id },
                (s, d) => d.Name = s.Name,
                removed => removedItem = removed);

            Assert.AreEqual(1, destination.Count);
            Assert.AreEqual("Item1", destination["a"].Name);
            Assert.IsNotNull(removedItem);
            Assert.AreEqual(2, removedItem.Id);
        }

        [TestMethod]
        public void RemoveWhereTest()
        {
            var dict = new Dictionary<string, int>
            {
                { "a", 1 },
                { "b", 2 },
                { "c", 3 },
                { "d", 4 }
            };

            dict.RemoveWhere(v => v % 2 == 0);

            Assert.AreEqual(2, dict.Count);
            Assert.IsTrue(dict.ContainsKey("a"));
            Assert.IsTrue(dict.ContainsKey("c"));
            Assert.IsFalse(dict.ContainsKey("b"));
            Assert.IsFalse(dict.ContainsKey("d"));
        }

        [TestMethod]
        public void RemoveWhereWithNoMatchesTest()
        {
            var dict = new Dictionary<string, int>
            {
                { "a", 1 },
                { "b", 3 }
            };

            dict.RemoveWhere(v => v % 2 == 0);

            Assert.AreEqual(2, dict.Count);
        }
    }
}
