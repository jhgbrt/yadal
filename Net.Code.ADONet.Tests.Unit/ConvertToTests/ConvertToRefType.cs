using System;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{

    public class ConvertToRefType
    {
        [Fact]
        public void FromNull_ShouldReturnNull()
        {
            string result = Convert(null);
            Assert.Null(result);
        }

        [Fact]
        public void FromString_ShouldReturnString()
        {
            object o = "";
            var result = Convert(o);
            Assert.Equal("", result);
        }

        [Fact]
        public void FromIncompatibleType_ShouldThrow()
        {
            object o = new object();
            Assert.Throws<InvalidCastException>(() => Convert(o));
        }

        [Fact]
        public void FromValueType_ShouldConvertToString()
        {
            int o = 1;
            var result = Convert(o);
            Assert.Equal("1", result);
        }

        [Fact]
        public void FromNullableType_DoesNotThrow()
        {
            int? o = 1;
            Assert.Equal("1", Convert(o));
        }

        [Fact]
        public void FromDbNull_ShouldReturnNull()
        {
            var result = Convert(DBNull.Value);
            Assert.Null(result);
        }

        private static string Convert(object o)
        {
            return ConvertTo<string>.From(o);
        }
    }
}
