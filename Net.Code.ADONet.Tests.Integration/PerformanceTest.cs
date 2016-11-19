using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Code.ADONet.Extensions.Experimental;

namespace Net.Code.ADONet.Tests.Integration
{
    [TestClass]
    public abstract class PerformanceTest
    {
        protected PerformanceTest()
        {
            _target = DbTargetFactory.Create(GetType().Name);
            _test = new DbTest(GetType().Name);
        }

        private readonly BaseDb _target;
        private readonly DbTest _test;

        [TestInitialize]
        public void Setup()
        {
            try
            {
                _test.Initialize();
            }
            catch (Exception e)
            {
                Assert.Inconclusive($"{_target.GetType().Name} - connnection failed {_target.ConnectionString} {e}");
            }
            _test.BulkInsert(FakeData.People.List(10000));
        }

        [TestCleanup]
        public void Cleanup()
        {
            _test.Cleanup();
        }


        [TestMethod]
        public void WhenMappingWithCachedSetterMap_ThenMappingIsFaster()
        {
            var fast = Measure(() => _test.GetAllPeopleGeneric());
            Trace.WriteLine(fast);

            var slow = Measure(() => _test.GetAllPeopleGenericLegacy());
            Trace.WriteLine(slow);

            Assert.IsTrue(slow > fast);
        }

        private static TimeSpan Measure(Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            return sw.Elapsed;
        }

        [TestClass]
        public class SqlServer : PerformanceTest
        {
        }
        [TestClass]
        public class Oracle : PerformanceTest
        {
        }

        [TestClass]
        public class SqlServerCe : PerformanceTest
        {
        }
        [TestClass]
        public class SqLite : PerformanceTest
        {
        }

        [TestClass]
        public class MySql : PerformanceTest
        {
        }

        [TestClass]
        public class PostgreSql : PerformanceTest
        {
        }
    }
}