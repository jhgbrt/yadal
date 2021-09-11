using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Net.Code.ADONet.Tests.Integration.Data;
using Net.Code.ADONet.Tests.Integration.Databases;
using Net.Code.ADONet.Tests.Integration.TestSupport;
using Xunit;
using Xunit.Abstractions;
using Net.Code.ADONet;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
namespace IntegrationTests
{
    [CollectionDefinition("Database collection")]
    public class DatabaseCollection
    {
    }

    [Collection("Database collection")]
    public abstract class DatabaseTest<T> : IDisposable where T : IDatabaseImpl, new()
    {
        private readonly DbTestHelper<T> _testHelper;
        private readonly Person[] _people;
        private readonly Address[] _addresses;
        private readonly ITestOutputHelper _output;


        protected DatabaseTest(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine($"{GetType()} - initialize");
            _people = FakeData.People.List(10).ToArray();
            _addresses = FakeData.Addresses.List(10);
            _testHelper = new DbTestHelper<T>(output);
            _testHelper.Initialize();
            _testHelper.Insert(_people.Take(5), _addresses);
            _testHelper.InsertAsync(_people.Skip(5)).Wait();
        }

        public void Dispose()
        {
            _testHelper.Cleanup();
        }

        [SkippableFact]
        public void UpdateAll()
        {
            var people = _testHelper.GetAllPeopleGeneric();
            foreach (var p in people)
            {
                p.RequiredNumber = 9999;
            }
            _testHelper.Update(people);
            people = _testHelper.GetAllPeopleGeneric();
            Assert.True(people.All(p => p.RequiredNumber == 9999));
        }

        [SkippableFact]
        public void CountIsExpected()
        {
            var count = _testHelper.GetCountOfPeople();
            Assert.Equal(10, count);
        }

        [SkippableFact]
        public async Task CountIsExpectedAsync()
        {
            var count = await _testHelper.GetCountOfPeopleAsync();
            Assert.Equal(10, count);
        }

        [SkippableFact]
        public void GetAllPeopleGeneric()
        {
            var result = _testHelper.GetAllPeopleGeneric();
            Assert.Equal(_people, result);
        }

        [SkippableFact]
        public void GetAllPeopleAsDynamic()
        {
            var result = _testHelper.GetAllPeopleAsDynamic();
            Assert.Equal(_people, result);
        }

        [SkippableFact]
        public async Task GetAllPeopleAsDynamicAsync()
        {
            var result = await _testHelper.GetAllPeopleAsDynamicAsync();
            Assert.Equal(_people, result);
        }

        [SkippableFact]
        public void AsDataTable()
        {
            var result = _testHelper.PeopleAsDataTable();
            Assert.Equal(_people.Select(p => p.Id).ToArray(), result.Rows.OfType<DataRow>().Select(dr => (int)dr["Id"]).ToArray());
            var columnName = _testHelper.GetColumnName(nameof(Person.OptionalNumber));
            Assert.Equal(_people.Select(p => p.OptionalNumber).ToArray(), result.Rows.OfType<DataRow>().Select(dr => dr.Field<int?>(columnName)).ToArray());
        }

        [SkippableFact]
        public void MultiResultSet()
        {
            var result = _testHelper.AsMultiResultSet();
            Assert.Equal(_people, result.Item1.ToArray());
            Assert.Equal(_addresses, result.Item2.ToArray());
        }

        [SkippableFact]
        public void GetSchemaTable()
        {
            var dt = _testHelper.GetSchemaTable();
            foreach (DataColumn dc in dt.Columns)
            {
                Console.WriteLine($"{dc.ColumnName} ({dc.DataType})");
            }
        }

        [SkippableFact]
        public void InsertAndGet()
        {
            var person = FakeData.People.One();
            _testHelper.Insert(new[] { person }, Enumerable.Empty<Address>());
            var result = _testHelper.Get(person.Id);
            Assert.Equal(person, result);
        }

        [SkippableFact]
        public async Task InsertAndGetAsync()
        {
            var person = FakeData.People.One();
            await _testHelper.InsertAsync(new[] { person }, Enumerable.Empty<Address>());
            var result = await _testHelper.GetAsync(person.Id);
            Assert.Equal(person, result);
        }

        [SkippableFact]
        public void GetByIdList()
        {
            var (ids, result) = _testHelper.GetByIdList();
            Assert.Equal(ids, result.Select(p => p.Id).ToList());
        }

        [SkippableFact]
        public void BulkCopy()
        {
            _testHelper.BulkInsert(FakeData.People.List(100));
        }
    }
    namespace Database
    {
        [Trait("Database", "SQLSERVER")]
        public class SqlServer : DatabaseTest<SqlServerDb> { public SqlServer(ITestOutputHelper output) : base(output) { } }
        //[Trait("Database", "ORACLE")]
        //public class Oracle : DatabaseTest<OracleDb> { public Oracle(ITestOutputHelper output) : base(output) { } }
        [Trait("Database", "SQLITE")]
        public class SqLite : DatabaseTest<SqLiteDb> { public SqLite(ITestOutputHelper output) : base(output) { } }
        [Trait("Database", "MSSQLITE")]
        public class MSSqLite : DatabaseTest<SqLiteDb> { public MSSqLite(ITestOutputHelper output) : base(output) { } }
        [Trait("Database", "MYSQL")]
        public class MySql : DatabaseTest<MySqlDb> { public MySql (ITestOutputHelper output) : base(output) { } }
        [Trait("Database", "POSTGRES")]
        public class Postgres : DatabaseTest<PostgreSqlDb> { public Postgres(ITestOutputHelper output) : base(output) { } }

        //[Trait("Database", "DB2")]
        //public class DB2 : DatabaseTest<DB2Db> { public DB2(ITestOutputHelper output) : base(output) { } }
    }
}
