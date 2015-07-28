using System;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.TypeExtensionsTests
{
    [TestFixture]
    public class DBNullHelperTests
    {
        [Test]
        public void IsNullable_ForNullableType_ShouldBeFalse()
        {
            Assert.IsTrue(DBNullHelper.IsNullableType(typeof (int?)));
        }

        [Test]
        public void IsNullable_ForValueType_ShouldBeFalse()
        {
            Assert.IsFalse(DBNullHelper.IsNullableType(typeof(int)));
        }

        [Test]
        public void IsNullable_ForReferenceType_ShouldBeFalse()
        {
            Assert.IsFalse(DBNullHelper.IsNullableType(typeof(string)));
        }

        [Test]
        public void IsNull_NullObject_ShouldBeTrue()
        {
            object o = null;
            Assert.IsTrue(DBNullHelper.IsNull(o));
        }

        [Test]
        public void IsNull_DBNullValue_ShouldBeTrue()
        {
            Assert.IsTrue(DBNullHelper.IsNull(DBNull.Value));
        }

        [Test]
        public void IsNull_NonNullObject_ShouldBeFalse()
        {
            object o = new object();
            Assert.IsFalse(DBNullHelper.IsNull(o));
        }

        [Test]
        public void IsNull_ValueType_ShouldBeFalse()
        {
            int i = 0;
            Assert.IsFalse(DBNullHelper.IsNull(i));
        }

        [Test]
        public void IsNull_NullNullableValue_ShouldBeTrue()
        {
            int? i = null;
            Assert.IsTrue(DBNullHelper.IsNull(i));
        }

        [Test]
        public void IsNull_NonNullNullableValue_ShouldBeFalse()
        {
            int? i = 0;
            Assert.IsFalse(DBNullHelper.IsNull(i));
        }

        [Test]
        public void FromDb_DBNullValue_ShouldBeNull()
        {
            object o = DBNullHelper.FromDb(DBNull.Value);
            Assert.IsNull(o);
        }

        [Test]
        public void FromDb_NullReference_ShouldBeNull()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = DBNullHelper.FromDb(input);
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.IsNull(o);
        }

        [Test]
        public void FromDb_NonNullString_ShouldStaySame()
        {
            object input = "xyz";
            object o = DBNullHelper.FromDb(input);
            Assert.AreEqual(input, o);
        }

        [Test]
        public void FromDb_BoxedInteger_ShouldStaySame()
        {
            object input = 1;
            object o = DBNullHelper.FromDb(input);
            Assert.AreEqual(input, o);
        }

        [Test]
        public void ToDb_DBNullValue_ShouldStayDBNull()
        {
            object o = DBNullHelper.ToDb(DBNull.Value);
            Assert.AreEqual(DBNull.Value, o);
        }

        [Test]
        public void ToDb_NullReference_ShouldBecomeDBNull()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = DBNullHelper.ToDb(input);
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.AreEqual(DBNull.Value, o);
        }

        [Test]
        public void ToDb_NonNullString_ShouldStaySame()
        {
            object input = "xyz";
            object o = DBNullHelper.ToDb(input);
            Assert.AreEqual(input, o);
        }

        [Test]
        public void ToDb_BoxedInteger_ShouldStaySame()
        {
            var input = 1;
            object o = DBNullHelper.ToDb(input);
            Assert.AreEqual(input, o);
        }

        [Test]
        public void ToDb_NullNullableInteger_ShouldBecomeDBNull()
        {
            int? value = null;
            // ReSharper disable ExpressionIsAlwaysNull
            object input = value;
            object o = DBNullHelper.ToDb(input);
            // ReSharper enable ExpressionIsAlwaysNull
            Assert.AreEqual(DBNull.Value, o);
        }

    }
}
