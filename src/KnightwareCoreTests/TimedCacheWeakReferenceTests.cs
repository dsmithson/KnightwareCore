using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Knightware
{
    [TestClass]
    public class TimedCacheWeakReferenceTests
    {
        public class TestObject
        {
            public string Name { get; set; }
        }

        [TestMethod]
        public async Task CacheExpirationTimeTest()
        {
            const int cacheSeconds = 2;

            var testObject = new TestObject() { Name = "My Test" };
            var weakReference = new TimedCacheWeakReference<TestObject>(testObject, TimeSpan.FromSeconds(cacheSeconds));
            Assert.IsTrue(weakReference.StrongReferenceAvailable, "No strong reference available");

            //Drop my strong refrence, and perform a GC
            testObject = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

            //We are well within our cache time, so we should still have a reference
            Assert.IsTrue(weakReference.StrongReferenceAvailable, "Lost reference before cache timeout expired");

            //Now wait past our cache second time, then do a GC and verify we've lost the reference
            await Task.Delay(TimeSpan.FromSeconds(cacheSeconds + 1));
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            Assert.IsFalse(weakReference.StrongReferenceAvailable, "Strong reference is still available after cache expiration");
        }
    }
}
