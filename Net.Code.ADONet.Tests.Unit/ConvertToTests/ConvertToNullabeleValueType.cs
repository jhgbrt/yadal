using System;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    [TestFixture]
    public class ConvertToNullabeleValueType
    {
        [Test]
        public void FromNull_ShouldReturnNull()
        {
            var result = Convert(null);
            Assert.IsNull(result);
        }

        [Test]
        public void FromDBNull_ShouldReturnNull()
        {
            var result = Convert(DBNull.Value);
            Assert.IsNull(result);
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

        private static int? Convert(object o)
        {
            return ConvertTo<int?>.From(o);
        }
    }
}