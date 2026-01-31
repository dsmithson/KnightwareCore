using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Knightware
{
    [TestClass]
    public class PropertyChangedBaseTests
    {
        private class TestPropertyChangedClass : PropertyChangedBase
        {
            private string _name;
            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }

            private int _value;
            public int Value
            {
                get => _value;
                set
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }

            public void RaisePropertyChangedExplicitly(string propertyName)
            {
                OnPropertyChanged(propertyName);
            }
        }

        [TestMethod]
        public void PropertyChangedEventRaisedTest()
        {
            var obj = new TestPropertyChangedClass();
            string changedPropertyName = null;

            obj.PropertyChanged += (sender, e) => changedPropertyName = e.PropertyName;

            obj.Name = "Test";
            Assert.AreEqual("Name", changedPropertyName);
        }

        [TestMethod]
        public void PropertyChangedWithExplicitNameTest()
        {
            var obj = new TestPropertyChangedClass();
            string changedPropertyName = null;

            obj.PropertyChanged += (sender, e) => changedPropertyName = e.PropertyName;

            obj.Value = 42;
            Assert.AreEqual("Value", changedPropertyName);
        }

        [TestMethod]
        public void PropertyChangedNotRaisedWithoutSubscriberTest()
        {
            var obj = new TestPropertyChangedClass();
            obj.Name = "Test";
        }

        [TestMethod]
        public void MultiplePropertyChangedEventsTest()
        {
            var obj = new TestPropertyChangedClass();
            int eventCount = 0;

            obj.PropertyChanged += (sender, e) => eventCount++;

            obj.Name = "Test1";
            obj.Name = "Test2";
            obj.Value = 1;

            Assert.AreEqual(3, eventCount);
        }
    }
}
