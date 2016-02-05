using System;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    [TestFixture]
    public class ConvertToValueType
    {
        [Test]
        public void FromNull_ShouldThrow()
        {
            Assert.Throws<NullReferenceException>(() => Convert(null));
        }

        [Test]
        public void FromDbNull_ShouldThrow()
        {
            Assert.Throws<NullReferenceException>(() => Convert(DBNull.Value));
        }

        [Test]
        public void FromString_ShouldThrow()
        {
            object o = "";
            Assert.Throws<FormatException>(() => Convert(o));
        }

        [Test]
        public void FromIncompatibleRefType_ShouldThrow()
        {
            object o = new object();
            Assert.Throws<InvalidCastException>(() => Convert(o));
        }

        [Test]
        public void FromIncompatibleValueType_ShouldThrow()
        {
            object o = new object();
            Assert.Throws<InvalidCastException>(() => Convert(o));
        }

        [Test]
        public void FromCompatibleValueType_ShouldReturnValue()
        {
            int o = 1;
            var result = Convert(o);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void FromCompatibleNullableType_ShouldReturnValue()
        {
            int? o = 1;
            var result = Convert(o);
            Assert.AreEqual(1, result);
        }

        private static int Convert(object o)
        {
            return ConvertTo<int>.From(o);
        }
    }
}