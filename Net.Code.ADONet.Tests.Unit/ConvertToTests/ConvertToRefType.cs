using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    [TestClass]
    public class ConvertToRefType
    {
        [TestMethod]
        public void FromNull_ShouldReturnNull()
        {
            string result = Convert(null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void FromString_ShouldReturnString()
        {
            object o = "";
            var result = Convert(o);
            Assert.AreEqual("", result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void FromIncompatibleType_ShouldThrow()
        {
            object o = new object();
            Convert(o);
        }

        [TestMethod]
        public void FromValueType_ShouldConvertToString()
        {
            int o = 1;
            var result = Convert(o);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void FromNullableType_DoesNotThrow()
        {
            int? o = 1;
            Assert.AreEqual("1", Convert(o));
        }

        [TestMethod]
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
