using System;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    [TestFixture]
    public class ConvertToRefType
    {
        [Test]
        public void FromNull_ShouldReturnNull()
        {
            string result = Convert(null);
            Assert.IsNull(result);
        }

        [Test]
        public void FromString_ShouldReturnString()
        {
            object o = "";
            var result = Convert(o);
            Assert.AreEqual("", result);
        }

        [Test, ExpectedException(typeof(InvalidCastException))]
        public void FromIncompatibleType_ShouldThrow()
        {
            object o = new object();
            Convert(o);
        }

        [Test, ExpectedException(typeof(InvalidCastException))]
        public void FromIncompatibleValueType_ShouldThrow()
        {
            int o = 1;
            Convert(o);
        }

        [Test, ExpectedException(typeof(InvalidCastException))]
        public void FromIncompatibleNullableType_ShouldThrow()
        {
            int? o = 1;
            Convert(o);
        }

        [Test]
        public void FromDbNull_ShouldReturnNull()
        {
            var result = Convert(DBNull.Value);
            Assert.IsNull(result);
        }

        private static string Convert(object o)
        {
            return ConvertTo<string>.From(o);
        }
    }
}
