using System;
using System.Diagnostics;
using Net.Code.ADONet.Tests.Integration.Data;
using Net.Code.ADONet.Tests.Integration.Databases;
using Net.Code.ADONet.Tests.Integration.TestSupport;
using Xunit;
using Xunit.Abstractions;
using Oracle = Net.Code.ADONet.Tests.Integration.Databases.OracleDb;

namespace IntegrationTests
{
    

    [Collection("Database collection")]
    public abstract class PerformanceTest : IDisposable
    {
        private ITestOutputHelper _output;
        protected PerformanceTest(
            IDatabaseImpl databaseImpl, 
            AssemblyLevelInit init, ITestOutputHelper output)
        {
            var isAvailable = init.IsAvailable(databaseImpl);
            Skip.IfNot(isAvailable);

            _output = output;
            _testHelper = new DbTestHelper(databaseImpl);


            _testHelper.Initialize();
            _testHelper.BulkInsert(FakeData.People.List(10000));
        }

        private readonly DbTestHelper _testHelper;


        public void Dispose()
        {
            _testHelper.Cleanup();
        }


        [SkippableFact]
        public void WhenMappingWithCachedSetterMap_ThenMappingIsFaster()
        {
            _testHelper.GetAllPeopleGeneric();
            _testHelper.GetAllPeopleGenericLegacy();
            

            var fast = Measure(() => _testHelper.GetAllPeopleGeneric());
            _output.WriteLine(fast.ToString());

            var slow = Measure(() => _testHelper.GetAllPeopleGenericLegacy());
            _output.WriteLine(slow.ToString());

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
            public SqlServerTest(AssemblyLevelInit init, ITestOutputHelper output) : base(new SqlServerDb(), init, output)
            {
            }
        }
        public class OracleTest : PerformanceTest
        {
            public OracleTest(AssemblyLevelInit init, ITestOutputHelper output) : base(new OracleDb(), init, output)
            {
            }
        }

        public class SqlServerCeTest : PerformanceTest
        {
            public SqlServerCeTest(AssemblyLevelInit init, ITestOutputHelper output) : base(new SqlServerCeDb(), init, output)
            {
            }
        }
        public class SqLiteTest : PerformanceTest
        {
            public SqLiteTest(AssemblyLevelInit init, ITestOutputHelper output) : base(new SqLiteDb(), init, output)
            {
            }
        }
        public class MySqlTest : PerformanceTest
        {
            public MySqlTest(AssemblyLevelInit init, ITestOutputHelper output) : base(new MySqlDb(), init, output)
            {
            }
        }
        public class PostgreSqlTest : PerformanceTest
        {
            public PostgreSqlTest(AssemblyLevelInit init, ITestOutputHelper output) : base(new PostgreSqlDb(), init, output)
            {
            }
        }
    }
}