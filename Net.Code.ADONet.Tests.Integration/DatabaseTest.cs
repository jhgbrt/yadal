using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

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
        private readonly AssemblyLevelInit _init;
        //private readonly BaseDb target;
        private readonly DbTest test;
        private Person[] people;
        private Address[] addresses;
        private DbConfig config;

       
        protected DatabaseTest(IDatabaseImpl databaseImpl, AssemblyLevelInit init)
        {
            _init = init;
            var isAvailable = _init.IsAvailable(databaseImpl);

            Skip.IfNot(isAvailable);

            test = new DbTest(databaseImpl);
            config = DbConfig.FromProviderName(test.ProviderName);


            test.Initialize();
            people = FakeData.People.List(10).ToArray();
            addresses = FakeData.Addresses.List(10);
            test.Insert(people.Take(5), addresses);
            test.InsertAsync(people.Skip(5)).Wait();
        }

        public void Dispose()
        {
            test.Cleanup();
        }

        [Fact]
        public void UpdateAll()
        {
            var people = test.GetAllPeopleGeneric();
            foreach (var p in people)
            {
                p.RequiredNumber = 9999;
            }
            test.Update(people);
            people = test.GetAllPeopleGeneric();
            Assert.True(people.All(p => p.RequiredNumber == 9999));
        }

        [Fact]
        public void CountIsExpected()
        {
            var count = test.GetCountOfPeople();
            Assert.Equal(10, count);

        }

        [Fact]
        public async Task CountIsExpectedAsync()
        {
            var count = await test.GetCountOfPeopleAsync();
            Assert.Equal(10, count);

        }

        [Fact]
        public void GetAllPeopleGeneric()
        {
            var result = test.GetAllPeopleGeneric();
            Assert.Equal(people, result);
        }

        [Fact]
        public void GetAllPeopleAsDynamic()
        {
            var result = test.GetAllPeopleAsDynamic();
            Assert.Equal(people, result);
        }

        [Fact]
        public async Task GetAllPeopleAsDynamicAsync()
        {
            var result = await test.GetAllPeopleAsDynamicAsync();
            Assert.Equal(people, result);
        }

        [Fact]
        public void AsDataTable()
        {
            var result = test.AsDataTable();
            Assert.Equal(people.Select(p => p.Id).ToArray(), result.Rows.OfType<DataRow>().Select(dr => (int) dr["Id"]).ToArray());
            var columnName = config.MappingConvention.ToDb("OptionalNumber");
            Assert.Equal(people.Select(p => p.OptionalNumber).ToArray(), result.Rows.OfType<DataRow>().Select(dr => dr.Field<int?>(columnName)).ToArray());
        }

        [Fact]
        public void MultiResultSet()
        {
            if (test.SupportsMultipleResultSets)
            {
                var result = test.AsMultiResultSet();
                Assert.Equal(people, result.Set1.ToArray());
                Assert.Equal(addresses, result.Set2.ToArray());
            }
        }

        [Fact]
        public void GetSchemaTable()
        {
            var dt = test.GetSchemaTable();
            foreach (DataColumn dc in dt.Columns)
            {
                Console.WriteLine($"{dc.ColumnName} ({dc.DataType})");
            }
        }

        [Fact]
        public void InsertAndGet()
        {
            var person = FakeData.People.One();
            test.Insert(new[] { person }, Enumerable.Empty<Address>());
            var result = test.Get(person.Id);
            Assert.Equal(person, result);
        }

        [Fact]
        public void GetByIdList()
        {
            if (test.SupportsTableValuedParameters)
            {
                var ids = test.GetSomeIds(3);
                var result = test.GetPeopleById(ids);
                Assert.Equal(ids, result.Select(p => p.Id).ToList());
            }
        }

        [Fact]
        public void BulkCopy()
        {
            test.BulkInsert(FakeData.People.List(100));
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
