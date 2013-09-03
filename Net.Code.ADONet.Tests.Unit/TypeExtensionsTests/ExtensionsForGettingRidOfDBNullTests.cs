using System;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.TypeExtensionsTests
{
    [TestFixture]
    public class ExtensionsForGettingRidOfDBNullTests
    {
        [Test]
        public void null_IsNull()
        {
            object o = null;
            Assert.IsTrue(DBNullHelper.IsNull(o));
        }

        [Test]
        public void DBNull_IsNull()
        {
            Assert.IsTrue(DBNullHelper.IsNull(DBNull.Value));
        }

        [Test]
        public void non_null_reftype_is_not_null()
        {
            object o = new object();
            Assert.IsFalse(DBNullHelper.IsNull(o));
        }

        [Test]
        public void value_type_is_not_null()
        {
            int i = 0;
            Assert.IsFalse(DBNullHelper.IsNull(i));
        }

        [Test]
        public void nullable_value_type_without_value_is_null()
        {
            int? i = null;
            Assert.IsTrue(DBNullHelper.IsNull(i));
        }

        [Test]
        public void nullable_value_type_with_value_is_not_null()
        {
            int? i = 0;
            Assert.IsFalse(DBNullHelper.IsNull(i));
        }

        [Test]
        public void FromDb_converts_DBNull_to_null()
        {
            object o = DBNullHelper.FromDb(DBNull.Value);
            Assert.IsNull(o);
        }

        [Test]
        public void FromDb_converts_null_to_null()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = DBNullHelper.FromDb(input);
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.IsNull(o);
        }

        [Test]
        public void FromDb_leaves_string_values_untouched()
        {
            object input = "xyz";
            object o = DBNullHelper.FromDb(input);
            Assert.AreEqual(input, o);
        }

        [Test]
        public void FromDb_leaves_boxed_values_untouched()
        {
            object input = 1;
            object o = DBNullHelper.FromDb(input);
            Assert.AreEqual(input, o);
        }

        [Test]
        public void ToDb_converts_DbNull_to_DBNull()
        {
            object o = DBNullHelper.ToDb(DBNull.Value);
            Assert.AreEqual(DBNull.Value, o);
        }

        [Test]
        public void ToDb_converts_null_to_DbNull()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = DBNullHelper.ToDb(input);
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.AreEqual(DBNull.Value, o);
        }

        [Test]
        public void ToDb_leaves_string_values_untouched()
        {
            object input = "xyz";
            object o = DBNullHelper.ToDb(input);
            Assert.AreEqual(input, o);
        }

        [Test]
        public void ToDb_leaves_boxed_values_untouched()
        {
            var input = 1;
            object o = DBNullHelper.ToDb(input);
            Assert.AreEqual(input, o);
        }

    }
}
