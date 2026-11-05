using System.Threading.Tasks;
using Xunit;
namespace Net.Code.ADONet.Tests.Unit
{
    public class OverloadResolutionTests
    {
        public class Row { public string Name { get; set; } }
        CommandBuilder Create() { var c = new FakeCommand(new FakeConnection()); c.SetResultSet(new[] { new Row { Name = null } }); return new CommandBuilder(c, DbConfig.Default); }
        [Fact]
        public async Task UntypedLambda_BindsToDynamicOverload_SyncAndAsync()
        {
            object asyncResult = null;
            await foreach (var x in Create().AsEnumerableAsync(d => d["Name"])) asyncResult = x;
            object syncResult = null;
            foreach (var x in Create().AsEnumerable(d => d["Name"])) syncResult = x;
            Assert.Equal("sync=null async=null", $"sync={(syncResult is null ? "null" : syncResult.GetType().Name)} async={(asyncResult is null ? "null" : asyncResult.GetType().Name)}");
        }
    }
}
