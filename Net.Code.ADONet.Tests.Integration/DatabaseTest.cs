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
using System.Windows.Markup;

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
        protected readonly DbTestHelper<T> _testHelper;
        private readonly Person[] _people;
        private readonly Address[] _addresses;
        private readonly Product[] _products;
        private readonly ITestOutputHelper _output;


        protected DatabaseTest(ITestOutputHelper output, DatabaseFixture<T> fixture)
        {
            Skip.IfNot(fixture.IsAvailable, fixture.ConnectionFailureException?.Message);
            _output = output;
            _output.WriteLine($"{GetType()} - initialize");
            _people = FakeData.People.List(10).ToArray();
            _addresses = FakeData.Addresses.List(10);
            _products = FakeData.Products.List(10);
            _testHelper = new DbTestHelper<T>(fixture.Target, fixture.CreateDb(XUnitLogger.CreateLogger(output)));
            try
            {
                _testHelper.Initialize();
                _testHelper.Insert(
                    people: _people.Take(5),
                    addresses: _addresses,
                    products: _products);
                _testHelper.InsertAsync(
                    people: _people.Skip(5)
                    ).Wait();
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
                throw;
            }
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
        public void Delete()
        {
            var people = _testHelper.GetAllPeopleGeneric();
            var first = people.Take(1);
            _testHelper.Delete(first);
            var people2 = _testHelper.GetAllPeopleGeneric();
            Assert.DoesNotContain(first.Single(), people2);
        }

        [SkippableFact]
        public async Task DeleteAsync()
        {
            var people = _testHelper.GetAllPeopleGeneric();
            var first = people.Take(1);
            await _testHelper.DeleteAsync(people);
            var people2 = _testHelper.GetAllPeopleGeneric();
            Assert.DoesNotContain(first.Single(), people2);
        }

        [SkippableFact]
        public void SelectOne()
        {
            var key = _people.First().Id;
            var person = _testHelper.SelectOne(key);
            Assert.Equal(person, _people.First());
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
        public void GetAllProducts()
        {
            var result = _testHelper.GetAllProducts();
            Assert.Equal(_products, result);
        }

        [SkippableFact]
        public void GetAllAddresses()
        {
            var result = _testHelper.GetAllAddresses();
            Assert.Equal(_addresses, result);
        }

        [SkippableFact]
        public void AsDataTable()
        {
            var result = _testHelper.PeopleAsDataTable();
            Assert.Equal(_people.Select(p => p.Id).ToArray(), result.Rows.OfType<DataRow>().Select(dr => (int)dr["Id"]).ToArray());
            var columnName = _testHelper.GetColumnName<Person>(nameof(Person.OptionalNumber));
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
            Assert.NotNull(dt);
            foreach (DataColumn dc in dt.Columns)
            {
                _output.WriteLine($"{dc.ColumnName} ({dc.DataType})");
            }
        }

        [SkippableFact]
        public void InsertAndGet()
        {
            var person = FakeData.People.One();
            _testHelper.Insert(people: new[] { person });
            var result = _testHelper.Get(person.Id);
            Assert.Equal(person, result);
        }

        [SkippableFact]
        public async Task InsertAndGetAsync()
        {
            var person = FakeData.People.One();
            await _testHelper.InsertAsync(people: new[] { person });
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
        [SkippableFact]
        public async Task BulkCopyAsync()
        {
            await _testHelper.BulkInsertAsync(FakeData.People.List(100));
        }
    }

    namespace Database
    {
        [Trait("Database", "SQLSERVER")]
        public class SqlServer : DatabaseTest<SqlServerDb>, IClassFixture<DatabaseFixture<SqlServerDb>>
        {
            public SqlServer(DatabaseFixture<SqlServerDb> fixture, ITestOutputHelper output)
            : base(output, fixture) { }
            
        }
        [Trait("Database", "ORACLE")]
        public class Oracle : DatabaseTest<OracleDb>, IClassFixture<DatabaseFixture<OracleDb>>
        {
            public Oracle(DatabaseFixture<OracleDb> fixture, ITestOutputHelper output)
            : base(output, fixture) { }
        }
        [Trait("Database", "SQLITE")]
        public class SqLite : DatabaseTest<SqLiteDb>, IClassFixture<DatabaseFixture<SqLiteDb>>
        {
            public SqLite(DatabaseFixture<SqLiteDb> fixture, ITestOutputHelper output)
            : base(output, fixture) { }
        }
        [Trait("Database", "MSSQLITE")]
        public class MSSqLite : DatabaseTest<SqLiteDb>, IClassFixture<DatabaseFixture<SqLiteDb>>
        {
            public MSSqLite(DatabaseFixture<SqLiteDb> fixture, ITestOutputHelper output)
            : base(output, fixture) { }
        }
        [Trait("Database", "MYSQL")]
        public class MySql : DatabaseTest<MySqlDb>, IClassFixture<DatabaseFixture<MySqlDb>>
        {
            public MySql(DatabaseFixture<MySqlDb> fixture, ITestOutputHelper output)
            : base(output, fixture) { }
        }
        [Trait("Database", "POSTGRES")]
        public class Postgres : DatabaseTest<PostgreSqlDb>, IClassFixture<DatabaseFixture<PostgreSqlDb>>
        {
            public Postgres(DatabaseFixture<PostgreSqlDb> fixture, ITestOutputHelper output)
            : base(output, fixture) { }
        }
        //[Trait("Database", "DB2")]
        //public class DB2 : DatabaseTest<DB2Db>, IClassFixture<DatabaseFixture<DB2Db>>
        //{
        //    public DB2(DatabaseFixture<DB2Db> fixture, ITestOutputHelper output)
        //    : base(output, fixture) { }
        //}
    }
}

