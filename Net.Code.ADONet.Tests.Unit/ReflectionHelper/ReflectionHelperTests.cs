using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.ReflectionHelper
{
    public class MyTestEntity
    {
        public string SomeProperty { get; set; }
        public object DbNullValueProperty { get; set; }

        public int IntProperty { get; set; }
        public int? NullableIntProperty { get; set; }
    }

    [TestClass]
    public class ReflectionHelperGetterTests
    {
        private MyTestEntity _entity = new MyTestEntity
        {
            DbNullValueProperty = DBNull.Value,
            SomeProperty = "Hello World",
            IntProperty = 1,
            NullableIntProperty = 2
        };
        IReadOnlyDictionary<string, Func<MyTestEntity, object>> getters = FastReflection.Instance.GetGettersForType<MyTestEntity>();

        [TestMethod]
        public void Getter_ForStringProperty_ReturnsCorrectValue()
        {
            var s = getters[nameof(MyTestEntity.SomeProperty)](_entity);
            Assert.AreEqual("Hello World", s);
        }
        [TestMethod]
        public void Getter_ForDbNullProperty_ReturnsNull()
        {
            var s = getters[nameof(MyTestEntity.DbNullValueProperty)](_entity);
            Assert.IsNull(s);
        }
        [TestMethod]
        public void Getter_ForNullableProperty_ReturnsValue()
        {
            var s = getters[nameof(MyTestEntity.NullableIntProperty)](_entity);
            Assert.AreEqual(_entity.NullableIntProperty, s);
        }
        [TestMethod]
        public void Getter_ForIntProperty_ReturnsValue()
        {
            var s = getters[nameof(MyTestEntity.IntProperty)](_entity);
            Assert.AreEqual(_entity.IntProperty, s);
        }
    }
    [TestClass]
    public class ReflectionHelperSetterTests
    {
        private readonly MyTestEntity _entity = new MyTestEntity
        {
        };

        IReadOnlyDictionary<string, Action<MyTestEntity,object>> setters = FastReflection.Instance.GetSettersForType<MyTestEntity>();

        [TestMethod]
        public void Setter_ForStringProperty_ReturnsCorrectValue()
        {
            var s = setters[nameof(MyTestEntity.SomeProperty)];
            s(_entity, "Hello World");
            Assert.AreEqual("Hello World", _entity.SomeProperty);
        }
        [TestMethod]
        public void Setter_ForDbNullProperty_ReturnsNull()
        {
            setters[nameof(MyTestEntity.DbNullValueProperty)](_entity, new object());
            Assert.IsNotNull(_entity.DbNullValueProperty);
        }
        [TestMethod]
        public void Setter_ForNullableProperty_ReturnsValue()
        {
            var s = setters[nameof(MyTestEntity.NullableIntProperty)];
            s(_entity, 1);
            Assert.AreEqual(_entity.NullableIntProperty, 1);
            s(_entity, null);
            Assert.IsNull(_entity.NullableIntProperty);
        }
        [TestMethod]
        public void Setter_ForIntProperty_ReturnsValue()
        {
            var s = setters[nameof(MyTestEntity.IntProperty)];
            s(_entity, 1);
            Assert.AreEqual(1, _entity.IntProperty);
        }
    }
}
