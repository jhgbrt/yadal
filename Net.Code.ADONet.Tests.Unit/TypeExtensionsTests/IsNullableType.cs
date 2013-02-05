using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.TypeExtensionsTests
{
    [TestFixture]
    public class IsNullableType
    {
        [Test]
        public void NullableType_True()
        {
            Assert.IsTrue(typeof (int?).IsNullableType());
        }

        [Test]
        public void ValueType_False()
        {
            Assert.IsFalse(typeof(int).IsNullableType());
        }

        [Test]
        public void RefType_False()
        {
            Assert.IsFalse(typeof(string).IsNullableType());
        }

    }
}
