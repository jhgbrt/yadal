using System;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.ConvertToTests
{

    public class ConvertToValueType
    {
        [Fact]
        public void FromNull_ShouldThrow()
        {
            Assert.Throws<NullReferenceException>(() => Convert(null));
        }

        [Fact]
        public void FromDbNull_ShouldThrow()
        {
            Assert.Throws<NullReferenceException>(() => Convert(DBNull.Value));
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
        public void FromIncompatibleValueType_ShouldThrow()
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

        private static int Convert(object o)
        {
            return ConvertTo<int>.From(o);
        }
    }
}