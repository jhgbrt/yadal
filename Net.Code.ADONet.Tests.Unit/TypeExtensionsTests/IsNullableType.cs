using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.TypeExtensionsTests
{
    [TestFixture]
    public class IsNullableType
    {
        [Test]
        public void NullableType_True()
        {
            Assert.IsTrue(DBNullHelper.IsNullableType(typeof (int?)));
        }

        [Test]
        public void ValueType_False()
        {
            Assert.IsFalse(DBNullHelper.IsNullableType(typeof(int)));
        }

        [Test]
        public void RefType_False()
        {
            Assert.IsFalse(DBNullHelper.IsNullableType(typeof(string)));
        }

    }
}
