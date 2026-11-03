using System.Data;
using System.Linq;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit
{
    public class CommandBuilderWithParametersTests
    {
        public class BaseParams
        {
            public int Id { get; set; }
        }
        public class DerivedParams : BaseParams
        {
            public string Name { get; set; }
        }

        private static CommandBuilder Create() => new(new FakeCommand(new FakeConnection()), DbConfig.Default);

        private static (string, object)[] Parameters(CommandBuilder b)
            => b.Command.Parameters.Cast<IDbDataParameter>().Select(p => (p.ParameterName, p.Value)).OrderBy(p => p.ParameterName).ToArray();

        [Fact]
        public void WithParameters_AnonymousObject_AddsParameters()
        {
            var b = Create().WithParameters(new { id = 1, name = "x" });
            Assert.Equal(new (string, object)[] { ("id", 1), ("name", "x") }, Parameters(b));
        }

        [Fact]
        public void WithParameters_AnonymousObjectTypedAsObject_AddsParameters()
        {
            object p = new { id = 1, name = "x" };
            var b = Create().WithParameters(p);
            Assert.Equal(new (string, object)[] { ("id", 1), ("name", "x") }, Parameters(b));
        }

        [Fact]
        public void WithParameters_DerivedInstanceTypedAsBase_AddsAllRuntimeProperties()
        {
            BaseParams p = new DerivedParams { Id = 2, Name = "y" };
            var b = Create().WithParameters(p);
            Assert.Equal(new (string, object)[] { ("Id", 2), ("Name", "y") }, Parameters(b));
        }

        [Fact]
        public void WithParameters_NullValue_AddsDbNull()
        {
            object p = new { name = (string)null };
            var b = Create().WithParameters(p);
            Assert.Equal(new (string, object)[] { ("name", System.DBNull.Value) }, Parameters(b));
        }
    }
}
