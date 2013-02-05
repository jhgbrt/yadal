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
            Assert.IsTrue(o.IsNull());
        }

        [Test]
        public void DBNull_IsNull()
        {
            Assert.IsTrue(DBNull.Value.IsNull());
        }

        [Test]
        public void non_null_reftype_is_not_null()
        {
            object o = new object();
            Assert.IsFalse(o.IsNull());
        }

        [Test]
        public void value_type_is_not_null()
        {
            int i = 0;
            Assert.IsFalse(i.IsNull());
        }

        [Test]
        public void nullable_value_type_without_value_is_null()
        {
            int? i = null;
            Assert.IsTrue(i.IsNull());
        }

        [Test]
        public void nullable_value_type_with_value_is_not_null()
        {
            int? i = 0;
            Assert.IsFalse(i.IsNull());
        }

        [Test]
        public void FromDb_converts_DBNull_to_null()
        {
            object o = DBNull.Value.FromDb();
            Assert.IsNull(o);
        }

        [Test]
        public void FromDb_converts_null_to_null()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = input.FromDb();
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.IsNull(o);
        }

        [Test]
        public void FromDb_leaves_string_values_untouched()
        {
            object input = "xyz";
            object o = input.FromDb();
            Assert.AreEqual(input, o);
        }

        [Test]
        public void FromDb_leaves_boxed_values_untouched()
        {
            object input = 1;
            object o = input.FromDb();
            Assert.AreEqual(input, o);
        }

        [Test]
        public void ToDb_converts_DbNull_to_DBNull()
        {
            object o = DBNull.Value.ToDb();
            Assert.AreEqual(DBNull.Value, o);
        }

        [Test]
        public void ToDb_converts_null_to_DbNull()
        {
            object input = null;
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            object o = input.ToDb();
            // ReSharper restore ConditionIsAlwaysTrueOrFalse
            Assert.AreEqual(DBNull.Value, o);
        }

        [Test]
        public void ToDb_leaves_string_values_untouched()
        {
            object input = "xyz";
            object o = input.ToDb();
            Assert.AreEqual(input, o);
        }

        [Test]
        public void ToDb_leaves_boxed_values_untouched()
        {
            var input = 1;
            object o = input.ToDb();
            Assert.AreEqual(input, o);
        }

    }
}
