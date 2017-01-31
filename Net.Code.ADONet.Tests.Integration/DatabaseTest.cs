using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
namespace Net.Code.ADONet.Tests.Integration
{
    [CollectionDefinition("Database collection")]
    public class DatabaseCollection 
        : ICollectionFixture<AssemblyLevelInit>
    {
    }

    [Collection("Database collection")]
    public abstract class DatabaseTest : IDisposable
    {
        private readonly IDatabaseImpl _databaseImpl;
        private readonly AssemblyLevelInit _init;
        //private readonly BaseDb target;
        private readonly DbTestHelper _testHelper;
        private Person[] people;
        private Address[] addresses;
        private DbConfig config;

       
        protected DatabaseTest(IDatabaseImpl databaseImpl, AssemblyLevelInit init)
        {
            _databaseImpl = databaseImpl;
            _init = init;
            var isAvailable = _init.IsAvailable(databaseImpl);

            Skip.IfNot(isAvailable);

            _testHelper = new DbTestHelper(databaseImpl);
            config = DbConfig.FromProviderName(databaseImpl.ProviderName);

            _testHelper.Initialize();
            people = FakeData.People.List(10).ToArray();
            addresses = FakeData.Addresses.List(10);
            _testHelper.Insert(people.Take(5), addresses);
            _testHelper.InsertAsync(people.Skip(5)).Wait();
        }

        public void Dispose()
        {
            _testHelper.Cleanup();
        }

        [Fact]
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

        [Fact]
        public void CountIsExpected()
        {
            var count = _testHelper.GetCountOfPeople();
            Assert.Equal(10, count);

        }

        [Fact]
        public async Task CountIsExpectedAsync()
        {
            var count = await _testHelper.GetCountOfPeopleAsync();
            Assert.Equal(10, count);

        }

        [Fact]
        public void GetAllPeopleGeneric()
        {
            var result = _testHelper.GetAllPeopleGeneric();
            Assert.Equal(people, result);
        }

        [Fact]
        public void GetAllPeopleAsDynamic()
        {
            var result = _testHelper.GetAllPeopleAsDynamic();
            Assert.Equal(people, result);
        }

        [Fact]
        public async Task GetAllPeopleAsDynamicAsync()
        {
            var result = await _testHelper.GetAllPeopleAsDynamicAsync();
            Assert.Equal(people, result);
        }

        [Fact]
        public void AsDataTable()
        {
            var result = _testHelper.AsDataTable();
            Assert.Equal(people.Select(p => p.Id).ToArray(), result.Rows.OfType<DataRow>().Select(dr => (int) dr["Id"]).ToArray());
            var columnName = config.MappingConvention.ToDb("OptionalNumber");
            Assert.Equal(people.Select(p => p.OptionalNumber).ToArray(), result.Rows.OfType<DataRow>().Select(dr => dr.Field<int?>(columnName)).ToArray());
        }

        [Fact]
        public void MultiResultSet()
        {
            if (_databaseImpl.SupportsMultipleResultSets)
            {
                var result = _testHelper.AsMultiResultSet();
                Assert.Equal(people, result.Set1.ToArray());
                Assert.Equal(addresses, result.Set2.ToArray());
            }
        }

        [Fact]
        public void GetSchemaTable()
        {
            var dt = _testHelper.GetSchemaTable();
            foreach (DataColumn dc in dt.Columns)
            {
                Console.WriteLine($"{dc.ColumnName} ({dc.DataType})");
            }
        }

        [Fact]
        public void InsertAndGet()
        {
            var person = FakeData.People.One();
            _testHelper.Insert(new[] { person }, Enumerable.Empty<Address>());
            var result = _testHelper.Get(person.Id);
            Assert.Equal(person, result);
        }

        [Fact]
        public void GetByIdList()
        {
            if (_databaseImpl.SupportsTableValuedParameters)
            {
                var ids = _testHelper.GetSomeIds(3);
                var result = _testHelper.GetPeopleById(ids);
                Assert.Equal(ids, result.Select(p => p.Id).ToList());
            }
        }

        [Fact]
        public void BulkCopy()
        {
            _testHelper.BulkInsert(FakeData.People.List(100));
        }

        public class SqlServerTest : DatabaseTest
        {
            public SqlServerTest(AssemblyLevelInit init) : base(new SqlServer(), init)
            {
            }
        }
        public class OracleTest : DatabaseTest
        {
            public OracleTest(AssemblyLevelInit init) : base(new Oracle(), init)
            {
            }
        }

        public class SqlServerCeTest : DatabaseTest
        {
            public SqlServerCeTest(AssemblyLevelInit init) : base(new SqlServerCe(), init)
            {
            }
        }
        public class SqLiteTest : DatabaseTest
        {
            public SqLiteTest(AssemblyLevelInit init) : base(new SqLite(), init)
            {
            }
        }
        public class MySqlTest : DatabaseTest
        {
            public MySqlTest(AssemblyLevelInit init) : base(new MySql(), init)
            {
            }
        }
        public class PostgreSqlTest : DatabaseTest
        {
            public PostgreSqlTest(AssemblyLevelInit init) : base(new PostgreSql(), init)
            {
            }
        }
    }






}
