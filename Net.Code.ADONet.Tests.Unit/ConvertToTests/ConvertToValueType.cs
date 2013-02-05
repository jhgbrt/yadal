using System;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    [TestFixture]
    public class ConvertToValueType
    {
        [Test, ExpectedException(typeof(NullReferenceException))]
        public void FromNull_ShouldThrow()
        {
            Convert(null);
        }

        [Test, ExpectedException(typeof(NullReferenceException))]
        public void FromDbNull_ShouldThrow()
        {
            Convert(DBNull.Value);
        }

        [Test, ExpectedException(typeof(InvalidCastException))]
        public void FromString_ShouldThrow()
        {
            object o = "";
            Convert(o);
        }

        [Test, ExpectedException(typeof(InvalidCastException))]
        public void FromIncompatibleRefType_ShouldThrow()
        {
            object o = new object();
            Convert(o);
        }

        [Test, ExpectedException(typeof(InvalidCastException))]
        public void FromIncompatibleValueType_ShouldThrow()
        {
            object o = new object();
            Convert(o);
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