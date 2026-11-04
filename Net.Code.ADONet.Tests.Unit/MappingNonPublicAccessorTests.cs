using System.Data;
using System.Linq;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit
{
    // Properties with a non-public setter or getter are skipped by reflection-based mapping
    // (only public accessors are used), instead of crashing the whole mapping.
    public class MappingNonPublicAccessorTests
    {
        public class WithPrivateSetter
        {
            public int Id { get; private set; } = -1;
            public string Name { get; set; } = "";
        }

        public class WithPrivateGetter
        {
            public int Id { get; set; }
            public string Secret { private get; set; } = "";
        }

        [Fact]
        public void MapFromRecord_SkipsPropertyWithPrivateSetter()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Rows.Add(5, "Alice");
            using var reader = table.CreateDataReader();
            var map = reader.GetMapper<WithPrivateSetter>(DbConfig.Default);

            Assert.True(reader.Read());
            var item = map(reader);
            Assert.Equal(-1, item.Id);
            Assert.Equal("Alice", item.Name);
        }

        [Fact]
        public void AsDataReader_SkipsValuesOfPropertyWithPrivateGetter()
        {
            using var reader = new[] { new WithPrivateGetter { Id = 1, Secret = "s" } }.AsDataReader();
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(reader.GetOrdinal("Id")));
        }
    }
}
