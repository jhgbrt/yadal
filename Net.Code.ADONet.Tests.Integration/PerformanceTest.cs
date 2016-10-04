using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Code.ADONet.Extensions.Experimental;

namespace Net.Code.ADONet.Tests.Integration
{
    [TestClass]
    public class PerformanceTest
    {
 
        [TestMethod]
        public void WhenMappingWithCachedSetterMap_ThenMappingIsFaster()
        {
            BaseDb target = new SqLite();
            using (var db = (Db)target.CreateDb())
            {
                db.Sql(target.CreatePersonTable).AsNonQuery();

                db.Insert(FakeData.People.List(10000));

                var selectAll = Query<Person>.Create(target.MappingConvention).SelectAll;

                var slow = DoQuery(() => db.Sql(selectAll).AsEnumerableLegacy<Person>(db.Config));
                Trace.WriteLine(slow);

                var fast = DoQuery(() => db.Sql(selectAll).AsEnumerable<Person>());
                Trace.WriteLine(fast);

                db.Sql(target.DropPersonTable).AsNonQuery();
                Assert.IsTrue(slow > fast);
            }

        }

        private static TimeSpan DoQuery(Func<IEnumerable<Person>> action)
        {
            TimeSpan slow;
            var stopwatch1 = Stopwatch.StartNew();
            try
            {
                action().ToList();
            }
            finally
            {
                slow = stopwatch1.Elapsed;
            }
            return slow;
        }
    }
}