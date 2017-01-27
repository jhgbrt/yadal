using System;
using System.Diagnostics;
using Net.Code.ADONet.Extensions.Experimental;
using Xunit;

namespace Net.Code.ADONet.Tests.Integration
{
    

    [Collection("Database collection")]
    public abstract class PerformanceTest : IDisposable
    {
        protected PerformanceTest(IDatabaseImpl databaseImpl, AssemblyLevelInit init)
        {
            _test = new DbTest(databaseImpl);

            var isAvailable = init.IsAvailable(databaseImpl);
            Skip.IfNot(isAvailable);

            _test.Initialize();
            _test.BulkInsert(FakeData.People.List(10000));
        }

        private readonly DbTest _test;


        public void Dispose()
        {
            _test.Cleanup();
        }


        [Fact]
        public void WhenMappingWithCachedSetterMap_ThenMappingIsFaster()
        {
            _test.GetAllPeopleGeneric();
            _test.GetAllPeopleGenericLegacy();
            

            var fast = Measure(() => _test.GetAllPeopleGeneric());
            Trace.WriteLine(fast);

            var slow = Measure(() => _test.GetAllPeopleGenericLegacy());
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