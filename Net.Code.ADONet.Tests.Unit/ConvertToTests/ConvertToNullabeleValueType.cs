using System;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{
    public class ConvertToNullabeleValueType
    {
        [Fact]
        public void FromNull_ShouldReturnNull()
        {
            var result = Convert(null);
            Assert.Null(result);
        }

        [Fact]
        public void FromDBNull_ShouldReturnNull()
        {
            var result = Convert(DBNull.Value);
            Assert.Null(result);
        }

        [Fact]
        public void FromString_ShouldThrow()
        {
            object o = "";
            Assert.Throws<FormatException>(() => Convert(o));
        }

        [Fact]
        public void FromIncompatibleRefType_ShouldThrow()
        {
            object o = new object();
            Assert.Throws<InvalidCastException>(() => Convert(o));
        }

        [Fact]
        public void FromCompatibleValueType_ShouldReturnValue()
        {
            int o = 1;
            var result = Convert(o);
            Assert.Equal(1, result);
        }

        [Fact]
        public void FromCompatibleNullableType_ShouldReturnValue()
        {
            int? o = 1;
            var result = Convert(o);
            Assert.Equal(1, result);
        }

        private static int? Convert(object o) => ConvertTo<int?>.From(o);
    }
}