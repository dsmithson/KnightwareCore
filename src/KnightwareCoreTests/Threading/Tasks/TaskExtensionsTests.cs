using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Knightware.Threading.Tasks
{
    [TestClass]
    public class TaskExtensionsTests
    {
        [TestMethod]
        public async Task AllSuccessWithAllTrueTest()
        {
            var task1 = Task.FromResult(true);
            var task2 = Task.FromResult(true);
            var task3 = Task.FromResult(true);

            var result = await TaskExtensions.AllSuccess(task1, task2, task3);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task AllSuccessWithOneFalseTest()
        {
            var task1 = Task.FromResult(true);
            var task2 = Task.FromResult(false);
            var task3 = Task.FromResult(true);

            var result = await TaskExtensions.AllSuccess(task1, task2, task3);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AllSuccessWithEmptyArrayTest()
        {
            var result = await TaskExtensions.AllSuccess();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AllSuccessWithNullArrayTest()
        {
            var result = await TaskExtensions.AllSuccess(null);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task AllSuccessWithSingleTrueTest()
        {
            var result = await TaskExtensions.AllSuccess(Task.FromResult(true));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task AllSuccessWithSingleFalseTest()
        {
            var result = await TaskExtensions.AllSuccess(Task.FromResult(false));
            Assert.IsFalse(result);
        }
    }
}
