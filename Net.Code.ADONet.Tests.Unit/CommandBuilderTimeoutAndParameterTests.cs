using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit
{
    public class CommandBuilderTimeoutTests
    {
        private static CommandBuilder Create() => new(new FakeCommand(new FakeConnection()), DbConfig.Default);

        [Fact]
        public void WithTimeout_SubSecond_RoundsUpInsteadOfDisablingTheTimeout()
        {
            var b = Create().WithTimeout(TimeSpan.FromMilliseconds(500));
            Assert.Equal(1, b.Command.CommandTimeout);
        }

        [Fact]
        public void WithTimeout_FractionalSeconds_RoundsUp()
        {
            var b = Create().WithTimeout(TimeSpan.FromSeconds(2.1));
            Assert.Equal(3, b.Command.CommandTimeout);
        }

        [Fact]
        public void WithTimeout_WholeSeconds_IsExact()
        {
            var b = Create().WithTimeout(TimeSpan.FromSeconds(30));
            Assert.Equal(30, b.Command.CommandTimeout);
        }

        [Fact]
        public void WithTimeout_Zero_MeansNoTimeout()
        {
            var b = Create().WithTimeout(TimeSpan.Zero);
            Assert.Equal(0, b.Command.CommandTimeout);
        }

        [Fact]
        public void WithTimeout_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create().WithTimeout(TimeSpan.FromSeconds(-1)));
        }
    }

    public class CommandBuilderParameterTests
    {
        public class WithIndexerAndWriteOnlyProperty
        {
            private string _secret = "";
            public int Id { get; set; }
            public string this[int i] => i.ToString();
            public string Secret { set => _secret = value; }
            public string PrivateGetter { private get; set; } = "";
            public override string ToString() => _secret + PrivateGetter;
        }

        private static CommandBuilder Create() => new(new FakeCommand(new FakeConnection()), DbConfig.Default);

        [Fact]
        public void WithParameters_TypeWithIndexerAndNonReadableProperties_OnlyAddsReadableProperties()
        {
            var b = Create().WithParameters(new WithIndexerAndWriteOnlyProperty { Id = 5 });
            var p = b.Command.Parameters.Cast<IDbDataParameter>().Select(x => (x.ParameterName, x.Value)).ToArray();
            Assert.Equal(new (string, object)[] { ("Id", 5) }, p);
        }

        [Fact]
        public void WithParameter_WithDbTypeAndSize_SetsTypeAndSize()
        {
            var b = Create().WithParameter("name", "abc", DbType.AnsiString, 50);
            var p = (IDbDataParameter)b.Command.Parameters["name"];
            Assert.Equal(DbType.AnsiString, p.DbType);
            Assert.Equal(50, p.Size);
            Assert.Equal("abc", p.Value);
        }

        [Fact]
        public void WithParameter_WithDbTypeAndNull_SetsDbNull()
        {
            var b = Create().WithParameter("name", null, DbType.String);
            var p = (IDbDataParameter)b.Command.Parameters["name"];
            Assert.Equal(DbType.String, p.DbType);
            Assert.Equal(DBNull.Value, p.Value);
        }

        [Fact]
        public void WithParameter_WithDbType_UpdatesExistingParameter()
        {
            var b = Create().WithParameter("id", 1).WithParameter("id", 2L, DbType.Int64);
            Assert.Single(b.Command.Parameters);
            var p = (IDbDataParameter)b.Command.Parameters["id"];
            Assert.Equal(DbType.Int64, p.DbType);
            Assert.Equal(2L, p.Value);
        }
    }

    public class CommandBuilderCancellationTests
    {
        [Fact]
        public async Task AsNonQueryAsync_CancelledToken_Throws()
        {
            var b = new CommandBuilder(new FakeCommand(new FakeConnection()), DbConfig.Default);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.AsNonQueryAsync(new CancellationToken(true)));
        }

        [Fact]
        public async Task AsScalarAsync_CancelledToken_Throws()
        {
            var b = new CommandBuilder(new FakeCommand(new FakeConnection()), DbConfig.Default);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => b.AsScalarAsync<int>(new CancellationToken(true)));
        }

        [Fact]
        public async Task AsEnumerableAsync_CancelledViaWithCancellation_Throws()
        {
            var command = new FakeCommand(new FakeConnection());
            command.SetResultSet(new[] { new Person { Id = 1 } });
            var b = new CommandBuilder(command, DbConfig.Default);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in b.AsEnumerableAsync<Person>().WithCancellation(new CancellationToken(true))) { }
            });
        }

        [Fact]
        public async Task AsReaderAsync_ReturnsDbDataReader()
        {
            var command = new FakeCommand(new FakeConnection());
            command.SetResultSet(new[] { new Person { Id = 1 } });
            var b = new CommandBuilder(command, DbConfig.Default);
            await using var reader = await b.AsReaderAsync();
            Assert.True(await reader.ReadAsync());
        }

        [Fact]
        public async Task AsEnumerableAsync_WithDataRecordSelector_MapsRows()
        {
            var command = new FakeCommand(new FakeConnection());
            command.SetResultSet(new[] { new Person { Id = 7 } });
            var b = new CommandBuilder(command, DbConfig.Default);
            var ids = new System.Collections.Generic.List<int>();
            await foreach (var id in b.AsEnumerableAsync((IDataRecord r) => r.GetInt32(r.GetOrdinal("Id")))) ids.Add(id);
            Assert.Equal(new[] { 7 }, ids);
        }
    }

    public class CaseInsensitiveMappingTests
    {
        public class Source
        {
            public int id { get; set; }
            public string NAME { get; set; }
        }
        public class Target
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class TargetWithCaseVariants
        {
            public string Name { get; set; }
            public string NAME { get; set; }
        }

        [Fact]
        public void AsEnumerable_ColumnNamesDifferInCase_AreMapped()
        {
            var command = new FakeCommand(new FakeConnection());
            command.SetResultSet(new[] { new Source { id = 3, NAME = "x" } });
            var result = new CommandBuilder(command, DbConfig.Default).AsEnumerable<Target>().Single();
            Assert.Equal(3, result.Id);
            Assert.Equal("x", result.Name);
        }

        [Fact]
        public void AsEnumerable_ExactMatchIsPreferred()
        {
            var command = new FakeCommand(new FakeConnection());
            command.SetResultSet(new[] { new Source { id = 3, NAME = "x" } });
            var result = new CommandBuilder(command, DbConfig.Default).AsEnumerable<TargetWithCaseVariants>().Single();
            Assert.Equal("x", result.NAME);
            Assert.Null(result.Name);
        }
    }

    public class DbLazyConnectionTests
    {
        [Fact]
        public void Sql_DoesNotOpenTheConnection()
        {
            var connection = new FakeConnection();
            var db = new Db(connection, DbConfig.Default);
            db.Sql("SELECT 1");
            Assert.Equal(ConnectionState.Closed, connection.State);
        }

        [Fact]
        public void Execute_OpensTheConnection()
        {
            var connection = new FakeConnection();
            var db = new Db(connection, DbConfig.Default);
            db.Execute("UPDATE x SET y = 1");
            Assert.Equal(ConnectionState.Open, connection.State);
        }
    }

    public class CrudParameterTests
    {
        public class Item
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string Name { get; set; }
            public string this[int i] => i.ToString();
        }

        private static (Db db, FakeConnection connection) CreateDb()
        {
            var connection = new FakeConnection();
            return (new Db(connection, DbConfig.Default), connection);
        }

        private static (string, object)[][] Executed(FakeConnection connection)
            => connection.Commands.SelectMany(c => c.ExecutedNonQueries).Select(p => p.OrderBy(x => x.name).ToArray()).ToArray();

        [Fact]
        public void Insert_DoesNotBindDatabaseGeneratedKey()
        {
            var (db, connection) = CreateDb();
            db.Insert(new[] { new Item { Id = 1, Name = "a" }, new Item { Id = 2, Name = "b" } });
            Assert.Equal(new[] { new (string, object)[] { ("Name", "a") }, new (string, object)[] { ("Name", "b") } }, Executed(connection));
        }

        [Fact]
        public void Update_BindsAllColumns()
        {
            var (db, connection) = CreateDb();
            db.Update(new[] { new Item { Id = 1, Name = "a" } });
            Assert.Equal(new[] { new (string, object)[] { ("Id", 1), ("Name", "a") } }, Executed(connection));
        }

        [Fact]
        public void Delete_OnlyBindsKey()
        {
            var (db, connection) = CreateDb();
            db.Delete(new[] { new Item { Id = 1, Name = "a" } });
            Assert.Equal(new[] { new (string, object)[] { ("Id", 1) } }, Executed(connection));
        }

        [Fact]
        public async Task InsertAsync_DoesNotBindDatabaseGeneratedKey()
        {
            var (db, connection) = CreateDb();
            await db.InsertAsync(new[] { new Item { Id = 1, Name = "a" } }, CancellationToken.None);
            Assert.Equal(new[] { new (string, object)[] { ("Name", "a") } }, Executed(connection));
        }

        [Fact]
        public void Query_IgnoresIndexer()
        {
            var query = QueryFactory<Item>.Get(MappingConvention.Default);
            Assert.Equal("INSERT INTO Item (Name) VALUES (@Name)", query.Insert);
            Assert.Equal("SELECT Id, Name FROM Item", query.SelectAll);
        }
    }
}
