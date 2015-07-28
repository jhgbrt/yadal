using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Sqlite
{
    public static class Data
    {
        public static MyObject Create()
        {
            return new MyObject
                   {
                       Id = 1,
                       StringNotNull = "TestString1",
                       StringNull = "TestString2",
                       NullableUniqueId = Guid.NewGuid(),
                       NonNullableUniqueId = Guid.NewGuid(),
                       NullableInt = 2,
                       NonNullableInt = 3
                   };
        }

    }

    [TestClass]
    public class SqlServer
    {
        [TestMethod]
        public void TestInitialize()
        {
            var connectionStringName = "sqlserver";

            using (var db = Db.FromConfig(connectionStringName))
            {
                db.Execute("SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE 1 = 0");
            }
        }
    }

    [TestClass]
    public class SqLiteTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            var connectionStringName = "sqlite";

            using (var db = Db.FromConfig(connectionStringName))
            {
                db.Execute("DROP TABLE IF EXISTS MyTable");
                db.Execute("CREATE TABLE MyTable (" +
                           "Id int not null, " +
                           "StringNotNull nvarchar(25) not null," +
                           "StringNull nvarchar(25) null," +
                           "NullableUniqueId uniqueidentifier null," +
                           "NonNullableUniqueId uniqueidentifier not null," +
                           "NullableInt int null," +
                           "NonNullableInt int not null" +
                           ")"
                    );
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            var connectionStringName = "sqlite";
            using (var db = Db.FromConfig(connectionStringName))
            {
                db.Execute("DROP TABLE MyTable");
            }

        }

        private static IEnumerable<MyObject> Select(string connectionStringName)
        {
            using (var db = Db.FromConfig(connectionStringName))
            {
                return db.Sql("SELECT Id, " +
                              "StringNotNull, " +
                              "StringNull, " +
                              "NullableUniqueId, " +
                              "NonNullableUniqueId," +
                              "NullableInt," +
                              "NonNullableInt " +
                              "FROM MyTable")
                    .AsEnumerable()
                    .Select(d => new MyObject
                    {
                        Id = d.Id,
                        StringNotNull = d.StringNotNull,
                        StringNull = d.StringNull,
                        NullableUniqueId = d.NullableUniqueId,
                        NonNullableUniqueId = d.NonNullableUniqueId,
                        NullableInt = d.NullableInt,
                        NonNullableInt = d.NonNullableInt
                    })
                    .ToList();
            }
        }
        private static IEnumerable<MyObject> SelectFast(string connectionStringName)
        {
            using (var db = Db.FromConfig(connectionStringName))
            {
                return db.Sql("SELECT Id, " +
                              "StringNotNull, " +
                              "StringNull, " +
                              "NullableUniqueId, " +
                              "NonNullableUniqueId," +
                              "NullableInt," +
                              "NonNullableInt " +
                              "FROM MyTable")
                    .AsEnumerable(d => new MyObject
                    {
                        Id = d.Id,
                        StringNotNull = d.StringNotNull,
                        StringNull = d.StringNull,
                        NullableUniqueId = d.NullableUniqueId,
                        NonNullableUniqueId = d.NonNullableUniqueId,
                        NullableInt = d.NullableInt,
                        NonNullableInt = d.NonNullableInt
                    })
                    .ToList();
            }
        }

        private static void Insert(string connectionStringName, MyObject myObject)
        {
            using (var db = Db.FromConfig(connectionStringName))
            {
                Insert(db, myObject);
            }
        }

        private static void Insert(Db db, MyObject myObject)
        {
            db.Sql("INSERT INTO MyTable(" +
                   "Id, " +
                   "StringNotNull, " +
                   "StringNull, " +
                   "NullableUniqueId, " +
                   "NonNullableUniqueId," +
                   "NullableInt," +
                   "NonNullableInt" +
                   ") VALUES (" +
                   "@Id, " +
                   "@StringNotNull, " +
                   "@StringNull, " +
                   "@NullableUniqueId," +
                   "@NonNullableUniqueId," +
                   "@NullableInt," +
                   "@NonNullableInt" +
                   ")")
                .WithParameters(
                    myObject
                ).AsNonQuery();
        }

        [TestMethod]
        public void An_inserted_object_can_be_selected()
        {
            var myObject = new MyObject
            {
                Id = 1,
                StringNotNull = "StringNotNull",
                StringNull = "StringNull",
                NonNullableUniqueId = Guid.NewGuid(),
                NullableUniqueId = Guid.NewGuid(),
                NullableInt = 2,
                NonNullableInt = 3
            };

            Insert("sqlite", myObject);

            var item = Select("sqlite").First();

            Assert.AreEqual(myObject, item);
        }
        [TestMethod]
        public void An_parameter_can_be_updated()
        {
            var myObject = new MyObject
            {
                Id = 1,
                StringNotNull = "StringNotNull",
                StringNull = "StringNull",
                NonNullableUniqueId = Guid.NewGuid(),
                NullableUniqueId = Guid.NewGuid(),
                NullableInt = 2,
                NonNullableInt = 3
            };

            Insert("sqlite", myObject);

            using (var db = Db.FromConfig("sqlite"))
            {
                var commandBuilder = db.Sql("UPDATE MyTable SET StringNull = @stringValue WHERE Id = @id")
                    .WithParameter("id", 1)
                    .WithParameter("stringValue", "StringNull updated");
                commandBuilder.AsNonQuery();
                commandBuilder.WithParameter("stringValue", "StringNull updated again");
                commandBuilder.AsNonQuery();
                var result = db.Sql("SELECT StringNull FROM MyTable where Id = 1").AsScalar<string>();
                Assert.AreEqual("StringNull updated again", result);
            }
        }

        

        [TestMethod]
        public void PerformanceTest()
        {
            Logger.Log = null;
            RunTest(1);
            var faster = RunTest(10000);
            Assert.IsTrue(faster);
        }

        private static bool RunTest(int count)
        {
            var objects = Enumerable.Range(1, count).Select(i =>
                new MyObject
                {
                    Id = i,
                    StringNotNull = "StringNotNull",
                    StringNull = "StringNull",
                    NonNullableUniqueId = Guid.NewGuid(),
                    NullableUniqueId = Guid.NewGuid(),
                    NullableInt = 2,
                    NonNullableInt = 3
                });

            using (var tx = new TransactionScope())
            using (var db = Db.FromConfig("sqlite"))
            {
                foreach (var myObject in objects)
                    Insert(db, myObject);
                tx.Complete();
            }

            var sw = Stopwatch.StartNew();
            SelectFast("sqlite");
            var fast = sw.Elapsed;

            sw = Stopwatch.StartNew();
            Select("sqlite");
            var slow = sw.Elapsed;

            Console.WriteLine(slow);
            Console.WriteLine(fast);

            var faster = fast < slow;
            return faster;
        }
    }
}
