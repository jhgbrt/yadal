using System;
using System.Diagnostics;

using Microsoft.Extensions.Logging.Abstractions;

using Net.Code.ADONet.Tests.Integration.Data;
using Net.Code.ADONet.Tests.Integration.Databases;
using Net.Code.ADONet.Tests.Integration.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Performance
{

    [Collection("Database collection")]
    public abstract class PerformanceTest<T> : IDisposable where T: IDatabaseImpl, new()
    {
        private ITestOutputHelper _output;
        protected PerformanceTest(ITestOutputHelper output, DatabaseFixture<T> target)
        {
            _output = output;
            _testHelper = new DbTestHelper<T>(target.Target, target.CreateDb(NullLogger.Instance));
            _testHelper.Initialize();
            _testHelper.BulkInsert(FakeData.People.List(1000));
        }

        private readonly DbTestHelper<T> _testHelper;

        public void Dispose()
        {
            _output.WriteLine($"{GetType()} - cleanup");
            _testHelper.Cleanup();
        }

        [SkippableFact]
        public void WhenMappingWithCachedSetterMap_ThenMappingIsFaster()
        {
            _testHelper.GetAllPeopleGeneric();
            _testHelper.GetAllPeopleGenericLegacy();
            _testHelper.GetAllPeopleWithDataReaderMapper();

            var fast = Measure(
                () => { for (int i = 0; i < 100; i++) _testHelper.GetAllPeopleGeneric(); }
                );
            _output.WriteLine(fast.ToString());

            var slow = Measure(
                () => { for (int i = 0; i < 100; i++) _testHelper.GetAllPeopleGenericLegacy(); }
            );
            _output.WriteLine(slow.ToString());

            var evenfaster = Measure(
                () => { for (int i = 0; i < 100; i++) _testHelper.GetAllPeopleWithDataReaderMapper(); }
                );
            _output.WriteLine(evenfaster.ToString());

            //Assert.True(slow > fast, $"Mapping using cached setters is slower! (old method: {slow}, new method: {fast})");
            Assert.True(fast > evenfaster, $"Mapping using cached setters is faster than source generated datarecord mapper!");
        }

        private static TimeSpan Measure(Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            return sw.Elapsed;
        }
    }

    namespace Performance
    {
        [Trait("Database", "SQLITE")]
        public class SqLite : PerformanceTest<SqLiteDb>, IClassFixture<DatabaseFixture<SqLiteDb>>
        {
            public SqLite(DatabaseFixture<SqLiteDb> fixture, ITestOutputHelper output) : base(output, fixture) { }
        }
        [Trait("Database", "SQLITE")]
        public class SqlServer: PerformanceTest<SqlServerDb>, IClassFixture<DatabaseFixture<SqlServerDb>>
        {
            public SqlServer(DatabaseFixture<SqlServerDb> fixture, ITestOutputHelper output) : base(output, fixture) { }
        }
    }
}