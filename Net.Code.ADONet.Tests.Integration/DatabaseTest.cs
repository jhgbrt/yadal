using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
namespace Net.Code.ADONet.Tests.Integration
{
    public abstract class DatabaseTest
    {
        private readonly BaseDb target;
        private readonly DbTest test;
        private Person[] people;
        private Address[] addresses;
        private DbConfig config;

        protected DatabaseTest()
        {
            var targetType = (
                from t in Assembly.GetExecutingAssembly().GetTypes()
                where typeof (BaseDb).IsAssignableFrom(t) && t.Name == GetType().Name
                select t
                ).Single();

            target = (BaseDb) Activator.CreateInstance(targetType);
            test = new DbTest(target);
            config = DbConfig.FromProviderName(target.ProviderName);
        }

       
        [TestInitialize]
        public void Setup()
        {
            try
            {
                test.Connect();
            }
            catch (Exception e)
            {
                Assert.Inconclusive($"{target.GetType().Name} - connnection failed {target.ConnectionString} {e}");
            }

            people = FakeData.People.List(10);
            addresses = FakeData.Addresses.List(10);
            test.CreateTables();
            test.Insert(people.Take(5), addresses);
            test.InsertAsync(people.Skip(5)).Wait();
        }

        [TestCleanup]
        public void Cleanup()
        {
            test.DropTables();
        }

        [TestMethod]
        public void UpdateAll()
        {
            var people = test.GetAllPeopleGeneric();
            foreach (var p in people)
            p.RequiredNumber = 9999;
            test.Update(people);
            people = test.GetAllPeopleGeneric();
            Assert.IsTrue(people.All(p => p.RequiredNumber == 9999));
        }

        [TestMethod]
        public void CountIsExpected()
        {
            var count = test.GetCountOfPeople();
            Assert.AreEqual(10, count);

        }

        [TestMethod]
        public async Task CountIsExpectedAsync()
        {
            var count = await test.GetCountOfPeopleAsync();
            Assert.AreEqual(10, count);

        }

        [TestMethod]
        public void GetAllPeopleGeneric()
        {
            var result = test.GetAllPeopleGeneric();
            CollectionAssert.AreEqual(people, result);
        }

        [TestMethod]
        public void GetAllPeopleAsDynamic()
        {
            var result = test.GetAllPeopleAsDynamic();
            CollectionAssert.AreEqual(people, result);
        }

        [TestMethod]
        public async Task GetAllPeopleAsDynamicAsync()
        {
            var result = await test.GetAllPeopleAsDynamicAsync();
            CollectionAssert.AreEqual(people, result);
        }

        [TestMethod]
        public void AsDataTable()
        {
            var result = test.AsDataTable();
            CollectionAssert.AreEqual(people.Select(p => p.Id).ToArray(), result.Rows.OfType<DataRow>().Select(dr => (int) dr["Id"]).ToArray());
            var columnName = config.MappingConvention.ToDb("OptionalNumber");
            CollectionAssert.AreEqual(people.Select(p => p.OptionalNumber).ToArray(), result.Rows.OfType<DataRow>().Select(dr => dr.Field<int?>(columnName)).ToArray());
        }

        [TestMethod]
        public void MultiResultSet()
        {
            if (target.SupportsMultipleResultSets)
            {
                var result = test.AsMultiResultSet();
                CollectionAssert.AreEqual(people, result.Set1.ToArray());
                CollectionAssert.AreEqual(addresses, result.Set2.ToArray());
            }
        }

        [TestMethod]
        public void GetSchemaTable()
        {
            var dt = test.GetSchemaTable();
            foreach (DataColumn dc in dt.Columns)
            {
                Console.WriteLine($"{dc.ColumnName} ({dc.DataType})");
            }
        }

        [TestMethod]
        public void InsertAndGet()
        {
            var person = FakeData.People.One();
            test.Insert(new[] { person }, Enumerable.Empty<Address>());
            var result = test.Get(person.Id);
            Assert.AreEqual(person, result);
        }

        [TestMethod]
        public void GetByIdList()
        {
            if (target.SupportsTableValuedParameters)
            {
                var ids = test.GetSomeIds(3);
                var result = test.GetPeopleById(ids);
                CollectionAssert.AreEqual(ids, result.Select(p => p.Id).ToList());
            }
        }

        [TestMethod]
        public void BulkCopy()
        {
            test.BulkInsert(FakeData.People.List(100));
        }

        [TestClass]
        public class SqlServer : DatabaseTest
        {
        }
        [TestClass]
        public class Oracle : DatabaseTest
        {
        }

        [TestClass]
        public class SqlServerCe : DatabaseTest
        {
        }
        [TestClass]
        public class SqLite : DatabaseTest
        {
        }

        [TestClass]
        public class MySql : DatabaseTest
        {
        }

        [TestClass]
        public class PostgreSql : DatabaseTest
        {
        }
    }






}
