using System;
using System.Diagnostics;
using Net.Code.ADONet.Tests.Integration.Data;
using Net.Code.ADONet.Tests.Integration.Databases;
using Net.Code.ADONet.Tests.Integration.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{

    [Collection("Database collection")]
    public abstract class PerformanceTest<T> : IDisposable where T: IDatabaseImpl, new()
    {
        private ITestOutputHelper _output;
        protected PerformanceTest(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine($"{GetType()} - initialize");
            _testHelper = new DbTestHelper<T>(output);
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


            var fast = Measure(
                () => { for (int i = 0; i < 100; i++) _testHelper.GetAllPeopleGeneric(); }
                );
            _output.WriteLine(fast.ToString());

            var slow = Measure(
                () => { for (int i = 0; i < 100; i++) _testHelper.GetAllPeopleGenericLegacy(); }
            );
            _output.WriteLine(slow.ToString());

            Assert.True(slow > fast, $"Mapping using cached setters is slower! (old method: {slow}, new method: {fast})");
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
        public class SqLite : PerformanceTest<SqLiteDb> { public SqLite(ITestOutputHelper output) : base(output) { } }
    }
}