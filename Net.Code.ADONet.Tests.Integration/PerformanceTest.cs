using System;
using System.Diagnostics;
using Xunit;

namespace Net.Code.ADONet.Tests.Integration
{
    

    [Collection("Database collection")]
    public abstract class PerformanceTest : IDisposable
    {
        private readonly AssemblyLevelInit _init;

        protected PerformanceTest(IDatabaseImpl databaseImpl, AssemblyLevelInit init)
        {
            _init = init;
            _testHelper = new DbTestHelper(databaseImpl);

            var isAvailable = _init.IsAvailable(databaseImpl);
            Skip.IfNot(isAvailable);

            _testHelper.Initialize();
            _testHelper.BulkInsert(FakeData.People.List(10000));
        }

        private readonly DbTestHelper _testHelper;


        public void Dispose()
        {
            _testHelper.Cleanup();
        }


        [Fact]
        public void WhenMappingWithCachedSetterMap_ThenMappingIsFaster()
        {
            _testHelper.GetAllPeopleGeneric();
            _testHelper.GetAllPeopleGenericLegacy();
            

            var fast = Measure(() => _testHelper.GetAllPeopleGeneric());
            Trace.WriteLine(fast);

            var slow = Measure(() => _testHelper.GetAllPeopleGenericLegacy());
            Trace.WriteLine(slow);

            Assert.True(slow > fast);
        }

        private static TimeSpan Measure(Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            return sw.Elapsed;
        }

        public class SqlServerTest : PerformanceTest
        {
            public SqlServerTest(AssemblyLevelInit init) : base(new SqlServer(), init)
            {
            }
        }
        public class OracleTest : PerformanceTest
        {
            public OracleTest(AssemblyLevelInit init) : base(new Oracle(), init)
            {
            }
        }

        public class SqlServerCeTest : PerformanceTest
        {
            public SqlServerCeTest(AssemblyLevelInit init) : base(new SqlServerCe(), init)
            {
            }
        }
        public class SqLiteTest : PerformanceTest
        {
            public SqLiteTest(AssemblyLevelInit init) : base(new SqLite(), init)
            {
            }
        }
        public class MySqlTest : PerformanceTest
        {
            public MySqlTest(AssemblyLevelInit init) : base(new MySql(), init)
            {
            }
        }
        public class PostgreSqlTest : PerformanceTest
        {
            public PostgreSqlTest(AssemblyLevelInit init) : base(new PostgreSql(), init)
            {
            }
        }
    }
}