using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Net.Code.ADONet.Tests.Integration.Data;
using Net.Code.ADONet.Tests.Integration.Databases;
using Net.Code.ADONet.Tests.Integration.TestSupport;
using Xunit;
using Net.Code.ADONet;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
namespace IntegrationTests
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
        private readonly DbTestHelper _testHelper;
        private readonly Person[] _people;
        private readonly Address[] _addresses;
        private readonly DbConfig _config;


        protected DatabaseTest(IDatabaseImpl databaseImpl, AssemblyLevelInit init)
        {
            _databaseImpl = databaseImpl;
            var isAvailable = init.IsAvailable(databaseImpl);

            Skip.IfNot(isAvailable);

            _testHelper = new DbTestHelper(databaseImpl);
            _config = DbConfig.FromProviderName(databaseImpl.ProviderName);

            _testHelper.Initialize();
            _people = FakeData.People.List(10).ToArray();
            _addresses = FakeData.Addresses.List(10);
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
            if (!_databaseImpl.SupportsMultipleResultSets)
                throw new SkipException($"{_databaseImpl.GetType().Name} does not support multiple result sets");
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
        public void GetByIdList()
        {
            if (!_databaseImpl.SupportsTableValuedParameters)
                throw new SkipException($"{_databaseImpl.GetType().Name} does not support table valued parameters");
            var ids = _testHelper.GetSomeIds(3);
            var result = _testHelper.GetPeopleById(ids);
            Assert.Equal(ids, result.Select(p => p.Id).ToList());
        }

        [SkippableFact]
        public void BulkCopy()
        {
            _testHelper.BulkInsert(FakeData.People.List(100));
        }

        public class SqlServerTest : DatabaseTest
        {
            public SqlServerTest(AssemblyLevelInit init) : base(new SqlServerDb(), init)
            {
            }
        }
        public class OracleTest : DatabaseTest
        {
            public OracleTest(AssemblyLevelInit init) : base(new OracleDb(), init)
            {
            }
        }

        public class SqlServerCeTest : DatabaseTest
        {
            public SqlServerCeTest(AssemblyLevelInit init) : base(new SqlServerCeDb(), init)
            {
            }
        }
        public class SqLiteTest : DatabaseTest
        {
            public SqLiteTest(AssemblyLevelInit init) : base(new SqLiteDb(), init)
            {
            }
        }
        public class MySqlTest : DatabaseTest
        {
            public MySqlTest(AssemblyLevelInit init) : base(new MySqlDb(), init)
            {
            }
        }
        public class PostgreSqlTest : DatabaseTest
        {
            public PostgreSqlTest(AssemblyLevelInit init) : base(new PostgreSqlDb(), init)
            {
            }
        }
    }
}
