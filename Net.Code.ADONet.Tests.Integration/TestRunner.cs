using System;
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
            RunTest(On.SqlServerCe());
        }
        [TestMethod]
        public void RunTestOnSqlServer()
        {
            RunTest(On.SqlServer());
        }
        [TestMethod]
        public void RunTestOnOracle()
        {
            RunTest(On.Oracle());
        }
        [TestMethod]
        public void RunTestOnSqLite()
        {
            RunTest(On.SqLite());
        }

        private void RunTest(On target)
        {
            var test = new DbTest(target);
            var people = MyFaker.People.List(100);
            RunTest(() => test.DropRecreate(), "Drop/Recreate database");
            RunTest(() => test.CreateTable(), "Create Table");
            RunTest(() => test.Insert(people), "Insert items");
            RunTest(() => test.AsEnumerableOf(), result => CollectionAssert.AreEqual(people, result), "Enumerable mapped to Person");
            if (target.SupportsMultiResultSet)
                RunTest(() => test.AsMultiResultSet(), result => CollectionAssert.AreEqual(people.Concat(result[0]).ToArray(), result[0].Concat(result[1]).ToArray()), "Multi result set");
            RunTest(() => test.DropTable(), "Drop Table");
        }

        private void RunTest(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Assert.Fail($"{scenario} failed: {e.GetType().Name} {e.Message}");
            }
        }
        private void RunTest<T>(Func<T> action, Action<T> verify, string scenario)
        {
            try
            {
                var result = action();
                verify(result);
            }
            catch (Exception e)
            {
                Assert.Fail($"{scenario} failed: {e.Message}");
            }
        }
    }
}
