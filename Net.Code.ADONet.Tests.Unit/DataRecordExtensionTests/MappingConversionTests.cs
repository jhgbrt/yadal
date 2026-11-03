#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.DataRecordExtensionTests
{
    public enum Kind
    {
        A = 1,
        B = 2
    }

    public class MappingConversionTests
    {
        public record IntRecord(int Id, int? Count, string? Name);

        public record EnumRecord(int Id, Kind Kind, Kind? NullableKind);

        public record ObjectRecord(int Id, object Value);

        public record BytesRecord(int Id, IEnumerable<byte> Data);

        public record NonNullableRecord(int Id, int Count, Kind Kind, Guid Key, string? Name);

        public class EnumEntity
        {
            public int Id { get; set; }
            public Kind Kind { get; set; }
            public Kind? NullableKind { get; set; }
        }

        private static readonly DbConfig Config = new DbConfig(c => { }, MappingConvention.Default);

        private static DataTable CreateTable(params (string name, Type type)[] columns)
        {
            var table = new DataTable();
            foreach (var (name, type) in columns)
                table.Columns.Add(name, type);
            return table;
        }

        private static T[] MapAll<T>(DataTable table)
        {
            using var reader = table.CreateDataReader();
            var map = reader.GetMapper<T>(Config);
            var result = new System.Collections.Generic.List<T>();
            while (reader.Read()) result.Add(map(reader));
            return result.ToArray();
        }

        [Fact]
        public void Record_WithInt64Columns_IsMapped()
        {
            var table = CreateTable(("Id", typeof(long)), ("Count", typeof(long)), ("Name", typeof(string)));
            table.Rows.Add(1L, 5L, "first");
            table.Rows.Add(2L, DBNull.Value, DBNull.Value);

            var result = MapAll<IntRecord>(table);

            Assert.Equal(new[] { new IntRecord(1, 5, "first"), new IntRecord(2, null, null) }, result);
        }

        [Fact]
        public void Record_WithColumnsInDifferentOrder_IsMapped()
        {
            var table = CreateTable(("Name", typeof(string)), ("Count", typeof(long)), ("Id", typeof(long)));
            table.Rows.Add("x", 3L, 7L);

            var result = MapAll<IntRecord>(table);

            Assert.Equal(new IntRecord(7, 3, "x"), result.Single());
        }

        [Fact]
        public void Record_WithEnumFromInt64_IsMapped()
        {
            var table = CreateTable(("Id", typeof(long)), ("Kind", typeof(long)), ("NullableKind", typeof(long)));
            table.Rows.Add(1L, 2L, 1L);
            table.Rows.Add(2L, 1L, DBNull.Value);

            var result = MapAll<EnumRecord>(table);

            Assert.Equal(new[] { new EnumRecord(1, Kind.B, Kind.A), new EnumRecord(2, Kind.A, null) }, result);
        }

        [Fact]
        public void Record_WithEnumFromString_IsMapped()
        {
            var table = CreateTable(("Id", typeof(int)), ("Kind", typeof(string)), ("NullableKind", typeof(string)));
            table.Rows.Add(1, "b", "A");

            var result = MapAll<EnumRecord>(table);

            Assert.Equal(new EnumRecord(1, Kind.B, Kind.A), result.Single());
        }

        [Fact]
        public void Class_WithEnumFromInt64_IsMapped()
        {
            var table = CreateTable(("Id", typeof(long)), ("Kind", typeof(long)), ("NullableKind", typeof(long)));
            table.Rows.Add(1L, 2L, 1L);
            table.Rows.Add(2L, 1L, DBNull.Value);

            var result = MapAll<EnumEntity>(table);

            Assert.Equal(Kind.B, result[0].Kind);
            Assert.Equal(Kind.A, result[0].NullableKind);
            Assert.Equal(Kind.A, result[1].Kind);
            Assert.Null(result[1].NullableKind);
        }

        [Fact]
        public void Class_WithEnumFromInt32_IsMapped()
        {
            var table = CreateTable(("Id", typeof(int)), ("Kind", typeof(int)), ("NullableKind", typeof(int)));
            table.Rows.Add(1, 2, 2);

            var result = MapAll<EnumEntity>(table).Single();

            Assert.Equal(Kind.B, result.Kind);
            Assert.Equal(Kind.B, result.NullableKind);
        }

        [Fact]
        public void Class_WithEnumFromString_IsMapped()
        {
            var table = CreateTable(("Id", typeof(int)), ("Kind", typeof(string)), ("NullableKind", typeof(string)));
            table.Rows.Add(1, "B", "a");
            table.Rows.Add(2, "a", DBNull.Value);

            var result = MapAll<EnumEntity>(table);

            Assert.Equal(Kind.B, result[0].Kind);
            Assert.Equal(Kind.A, result[0].NullableKind);
            Assert.Equal(Kind.A, result[1].Kind);
            Assert.Null(result[1].NullableKind);
        }

        [Fact]
        public void Record_WithObjectParameter_AcceptsNonConvertibleValue()
        {
            var id = Guid.NewGuid();
            var table = CreateTable(("Id", typeof(int)), ("Value", typeof(Guid)));
            table.Rows.Add(1, id);

            var result = MapAll<ObjectRecord>(table).Single();

            Assert.Equal(1, result.Id);
            Assert.Equal(id, result.Value);
        }

        [Fact]
        public void Record_WithObjectParameter_KeepsOriginalValueType()
        {
            var table = CreateTable(("Id", typeof(int)), ("Value", typeof(long)));
            table.Rows.Add(1, 42L);

            var result = MapAll<ObjectRecord>(table).Single();

            Assert.IsType<long>(result.Value);
            Assert.Equal(42L, result.Value);
        }

        [Fact]
        public void Record_WithInterfaceParameter_AcceptsAssignableValue()
        {
            var table = CreateTable(("Id", typeof(int)), ("Data", typeof(byte[])));
            table.Rows.Add(1, new byte[] { 1, 2, 3 });

            var result = MapAll<BytesRecord>(table).Single();

            Assert.Equal(new byte[] { 1, 2, 3 }, result.Data);
        }

        [Fact]
        public void Record_WithDbNullForNonNullableValueTypeParameters_MapsToDefault()
        {
            var table = CreateTable(("Id", typeof(int)), ("Count", typeof(long)), ("Kind", typeof(long)), ("Key", typeof(Guid)), ("Name", typeof(string)));
            table.Rows.Add(1, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

            var result = MapAll<NonNullableRecord>(table).Single();

            Assert.Equal(new NonNullableRecord(1, 0, default, Guid.Empty, null), result);
        }

        private class TableCommand : FakeCommand
        {
            private readonly DataTable _table;
            public TableCommand(DataTable table) : base(new FakeConnection()) => _table = table;
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => _table.CreateDataReader();
        }

        [Fact]
        public void AsEnumerable_RecordWithInt64AndEnum_IsMapped()
        {
            var table = CreateTable(("Id", typeof(long)), ("Kind", typeof(long)), ("NullableKind", typeof(long)));
            table.Rows.Add(1L, 2L, DBNull.Value);

            var result = new CommandBuilder(new TableCommand(table), DbConfig.Default).AsEnumerable<EnumRecord>().ToList();

            Assert.Equal(new EnumRecord(1, Kind.B, null), result.Single());
        }

        [Fact]
        public void AsEnumerable_ClassWithEnum_IsMapped()
        {
            var table = CreateTable(("Id", typeof(long)), ("Kind", typeof(long)), ("NullableKind", typeof(string)));
            table.Rows.Add(1L, 1L, "b");

            var result = new CommandBuilder(new TableCommand(table), DbConfig.Default).AsEnumerable<EnumEntity>().Single();

            Assert.Equal(Kind.A, result.Kind);
            Assert.Equal(Kind.B, result.NullableKind);
        }
    }

    public class ConvertToEnumTests
    {
        [Fact]
        public void FromInt64_ReturnsEnum() => Assert.Equal(Kind.A, ConvertTo<Kind>.From(1L));

        [Fact]
        public void FromInt32_ReturnsEnum() => Assert.Equal(Kind.B, ConvertTo<Kind>.From(2));

        [Fact]
        public void FromByte_ReturnsEnum() => Assert.Equal(Kind.B, ConvertTo<Kind>.From((byte)2));

        [Fact]
        public void FromDecimal_ReturnsEnum() => Assert.Equal(Kind.B, ConvertTo<Kind>.From(2m));

        [Fact]
        public void FromString_CaseInsensitive_ReturnsEnum() => Assert.Equal(Kind.B, ConvertTo<Kind>.From("b"));

        [Fact]
        public void FromEnum_ReturnsEnum() => Assert.Equal(Kind.B, ConvertTo<Kind>.From(Kind.B));

        [Fact]
        public void Nullable_FromDbNull_ReturnsNull() => Assert.Null(ConvertTo<Kind?>.From(DBNull.Value));

        [Fact]
        public void Nullable_FromNull_ReturnsNull() => Assert.Null(ConvertTo<Kind?>.From(null));

        [Fact]
        public void Nullable_FromInt64_ReturnsEnum() => Assert.Equal(Kind.A, ConvertTo<Kind?>.From(1L));

        [Fact]
        public void Nullable_FromString_ReturnsEnum() => Assert.Equal(Kind.B, ConvertTo<Kind?>.From("B"));

        [Fact]
        public void FromDbNull_Throws() => Assert.Throws<NullReferenceException>(() => ConvertTo<Kind>.From(DBNull.Value));

        [Fact]
        public void FromUnknownString_Throws() => Assert.Throws<ArgumentException>(() => ConvertTo<Kind>.From("nope"));
    }
}
