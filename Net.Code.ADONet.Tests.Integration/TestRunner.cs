using System;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
namespace Net.Code.ADONet.Tests.Integration
{
    [TestClass]
    public class TestRunner
    {
        [TestMethod]
        public void RunTestOnSqlCe()
        {
            RunTest(new SqlServerCe());
        }
        [TestMethod]
        public void RunTestOnSqlServer()
        {
            RunTest(new SqlServer());
        }
        [TestMethod]
        public void RunTestOnOracle()
        {
            RunTest(new Oracle());
        }
        [TestMethod]
        public void RunTestOnSqLite()
        {
            RunTest(new SqLite());
        }

        private void RunTest(IDatabaseImpl target)
        {
            var test = new DbTest(target);
            var people = FakeData.People.List(10);

            test.DropRecreate();

            test.CreateTable();

            test.Insert(people.Take(5));
            test.InsertAsync(people.Skip(5)).Wait();
            {
                var count = test.GetCountOfPeople();
                Assert.AreEqual(10, count);
            }
            {
                var count = test.GetCountOfPeopleAsync().Result;
                Assert.AreEqual(10, count);
            }
            {
                var result = test.GetAllPeopleGeneric();
                CollectionAssert.AreEqual(people, result);
            }
            {
                var result = test.GetAllPeopleAsDynamic();
                CollectionAssert.AreEqual(people, result);
            }
            {
                var result = test.GetAllPeopleAsDynamicAsync().Result;
                CollectionAssert.AreEqual(people, result);
            }
            {
                var result = test.AsDataTable();
                CollectionAssert.AreEqual(people.Select(p => p.Id).ToArray(), result.Rows.OfType<DataRow>().Select(dr => (int)dr["Id"]).ToArray());
            }
            if (target.SupportsMultipleResultSets)
            {
                var result = test.AsMultiResultSet();
                CollectionAssert.AreEqual(people.Concat(result[0]).ToArray(), result[0].Concat(result[1]).ToArray());
            }
            {
                var person = FakeData.People.One();
                test.Insert(new[] {person});
                var result = test.Get(person.Id);
                Assert.AreEqual(person, result);
            }

            if (target.SupportsTableValuedParameters)
            {
                var ids = test.GetSomeIds(3);
                var result = test.GetPeopleById(ids);
                CollectionAssert.AreEqual(ids, result.Select(p => p.Id).ToList());
            }

            test.DropTable();
        }

        private void RunTest(Action action)
        {
            action();
        }
        private void RunTest<T>(Func<T> action, Action<T> verify, string scenario)
        {
            var result = action();
            verify(result);
        }
    }
}
