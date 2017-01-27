using System;
using Xunit;


namespace Net.Code.ADONet.Tests.Unit.TypeExtensionsTests
{

    public class DBNullHelperTests
    {
        [Fact]
        public void IsNullable_ForNullableType_ShouldBeFalse()
        {
            Assert.True(typeof (int?).IsNullableType());
        }

        [Fact]
        public void IsNullable_ForValueType_ShouldBeFalse()
        {
            Assert.False(typeof(int).IsNullableType());
        }

        [Fact]
        public void IsNullable_ForReferenceType_ShouldBeFalse()
        {
            Assert.False(typeof(string).IsNullableType());
        }

        [Fact]
        public void IsNull_NullObject_ShouldBeTrue()
        {
            object o = null;
            Assert.True(DBNullHelper.IsNull(o));
        }

        [Fact]
        public void IsNull_DBNullValue_ShouldBeTrue()
        {
            Assert.True(DBNullHelper.IsNull(DBNull.Value));
        }

        [Fact]
        public void IsNull_NonNullObject_ShouldBeFalse()
        {
            object o = new object();
            Assert.False(DBNullHelper.IsNull(o));
        }

        [Fact]
        public void IsNull_ValueType_ShouldBeFalse()
        {
            int i = 0;
            Assert.False(DBNullHelper.IsNull(i));
        }

        [Fact]
        public void IsNull_NullNullableValue_ShouldBeTrue()
        {
            int? i = null;
            Assert.True(DBNullHelper.IsNull(i));
        }

        [Fact]
        public void IsNull_NonNullNullableValue_ShouldBeFalse()
        {
            int? i = 0;
            Assert.False(DBNullHelper.IsNull(i));
        }

        [Fact]
        public void FromDb_DBNullValue_ShouldBeNull()
        {
            object o = DBNullHelper.FromDb(DBNull.Value);
            Assert.Null(o);
        }

        [Fact]
        public void FromDb_NullReference_ShouldBeNull()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = DBNullHelper.FromDb(input);
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.Null(o);
        }

        [Fact]
        public void FromDb_NonNullString_ShouldStaySame()
        {
            object input = "xyz";
            object o = DBNullHelper.FromDb(input);
            Assert.Equal(input, o);
        }

        [Fact]
        public void FromDb_BoxedInteger_ShouldStaySame()
        {
            object input = 1;
            object o = DBNullHelper.FromDb(input);
            Assert.Equal(input, o);
        }

        [Fact]
        public void ToDb_DBNullValue_ShouldStayDBNull()
        {
            object o = DBNullHelper.ToDb(DBNull.Value);
            Assert.Equal(DBNull.Value, o);
        }

        [Fact]
        public void ToDb_NullReference_ShouldBecomeDBNull()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = DBNullHelper.ToDb(input);
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.Equal(DBNull.Value, o);
        }

        [Fact]
        public void ToDb_NonNullString_ShouldStaySame()
        {
            object input = "xyz";
            object o = DBNullHelper.ToDb(input);
            Assert.Equal(input, o);
        }

        [Fact]
        public void ToDb_BoxedInteger_ShouldStaySame()
        {
            var input = 1;
            object o = DBNullHelper.ToDb(input);
            Assert.Equal(input, o);
        }

        [Fact]
        public void ToDb_NullNullableInteger_ShouldBecomeDBNull()
        {
            int? value = null;
            // ReSharper disable ExpressionIsAlwaysNull
            object input = value;
            object o = DBNullHelper.ToDb(input);
            // ReSharper enable ExpressionIsAlwaysNull
            Assert.Equal(DBNull.Value, o);
        }

    }
}
