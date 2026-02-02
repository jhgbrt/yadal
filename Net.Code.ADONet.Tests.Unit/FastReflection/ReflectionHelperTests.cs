using System;
using System.Collections.Generic;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.FastReflection
{
    public class MyTestEntity
    {
        public string SomeProperty { get; set; }
        public object DbNullValueProperty { get; set; }
        public int IntProperty { get; set; }
        public int? NullableIntProperty { get; set; }
#pragma warning disable CA1822 // Mark members as static
        public string ReadOnly => "ReadOnlyValue";
        public string WriteOnly { set { } }
#pragma warning restore CA1822 // Mark members as static
    }

    public class ReflectionHelperGetterTests
    {
        private readonly MyTestEntity _entity = new MyTestEntity
        {
            DbNullValueProperty = DBNull.Value,
            SomeProperty = "Hello World",
            IntProperty = 1,
            NullableIntProperty = 2,
            WriteOnly = "WriteOnly"
        };
        private readonly IReadOnlyDictionary<string, Func<MyTestEntity, object>> getters
            = FastReflection<MyTestEntity>.Instance.GetGettersForType();

        [Fact]
        public void Getter_ForStringProperty_ReturnsCorrectValue()
        {
            var s = getters[nameof(MyTestEntity.SomeProperty)](_entity);
            Assert.Equal("Hello World", s);
        }
        [Fact]
        public void Getter_ForDbNullProperty_ReturnsNull()
        {
            var s = getters[nameof(MyTestEntity.DbNullValueProperty)](_entity);
            Assert.Null(s);
        }
        [Fact]
        public void Getter_ForNullableProperty_ReturnsValue()
        {
            var s = getters[nameof(MyTestEntity.NullableIntProperty)](_entity);
            Assert.Equal(_entity.NullableIntProperty, s);
        }
        [Fact]
        public void Getter_ForIntProperty_ReturnsValue()
        {
            var s = getters[nameof(MyTestEntity.IntProperty)](_entity);
            Assert.Equal(_entity.IntProperty, s);
        }
        [Fact]
        public void Getter_ForReadonlyProperty_ReturnsValue()
        {
            var s = getters[nameof(MyTestEntity.ReadOnly)](_entity);
            Assert.Equal(_entity.ReadOnly, s);
        }
        [Fact]
        public void Getter_ForWriteOnlyProperty_DoesNotExist()
        {
            Assert.DoesNotContain(nameof(MyTestEntity.WriteOnly), getters);
        }
    }

    public class ReflectionHelperSetterTests
    {
        private readonly MyTestEntity _entity = new MyTestEntity();

        private readonly IReadOnlyDictionary<string, Action<MyTestEntity,object>> setters = FastReflection<MyTestEntity>.Instance.GetSettersForType(MappingConvention.Default);

        [Fact]
        public void Setter_ForStringProperty_ReturnsCorrectValue()
        {
            var s = setters[nameof(MyTestEntity.SomeProperty)];
            s(_entity, "Hello World");
            Assert.Equal("Hello World", _entity.SomeProperty);
        }
        [Fact]
        public void Setter_ForDbNullProperty_ReturnsNull()
        {
            setters[nameof(MyTestEntity.DbNullValueProperty)](_entity, new object());
            Assert.NotNull(_entity.DbNullValueProperty);
        }
        [Fact]
        public void Setter_ForNullableProperty_ReturnsValue()
        {
            var s = setters[nameof(MyTestEntity.NullableIntProperty)];
            s(_entity, 1);
            Assert.Equal(1, _entity.NullableIntProperty);
            s(_entity, null);
            Assert.Null(_entity.NullableIntProperty);
        }
        [Fact]
        public void Setter_ForIntProperty_ReturnsValue()
        {
            var s = setters[nameof(MyTestEntity.IntProperty)];
            s(_entity, 1);
            Assert.Equal(1, _entity.IntProperty);
        }
        [Fact]
        public void Setter_ForWriteOnlyProperty_DoesNotThrow()
        {
            var s = setters[nameof(MyTestEntity.WriteOnly)];
            s(_entity, "asdf");
        }
        [Fact]
        public void Setter_ForReadOnlyProperty_DoesNotExist()
        {
            Assert.DoesNotContain(nameof(MyTestEntity.ReadOnly), setters);
        }
    }
}
