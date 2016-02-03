using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net.Code.ADONet;

namespace SqlMapper
{
    [TestClass]
    public class PerfTest
    {
        [TestMethod]
        public void DoPerfTest_SelectOne()
        {
            //Logger.Log = Trace.WriteLine;
            Logger.Log = null;
            var nofRecords = 1000;
            Ensure(nofRecords);
            Run(1000, PerformanceTests.SelectSingleRecordTests);
        }

        [TestMethod]
        public void DoPerfTest_SelectAll()
        {
            Logger.Log = null;
            var nofRecords = 1000;
            Ensure(nofRecords);
            Run(2, PerformanceTests.SelectAllRecordsTests);
        }

        [TestMethod]
        public void MapperPerformance()
        {
            using (var db = Db.FromConfig("sqlite"))
            {
                db.Configure().WithMappingConvention(MappingConvention.Strict);
                var list = db.Sql("select * from Posts")
                    .AsEnumerable<Post>().ToList();
                list = db.Sql("select * from Posts")
                    .AsEnumerable<Post>().ToList();
                list = db.Sql("select * from Posts")
                    .AsEnumerable<Post>().ToList();
                Console.WriteLine(list.Count);
            }
        }

        private static void Ensure(int nofRecords)
        {
            using (var db = Db.FromConfig("sqlite"))
            {
                db.Execute(@"create table if not exists Posts (
    Id int primary key, [Text] varchar(2000) not null, CreationDate datetime not null, LastChangeDate datetime not null,
	Counter1 int, Counter2 int, Counter3 int, Counter4 int, Counter5 int, Counter6 int, Counter7 int, Counter8 int, Counter9 int )");

                using (var tx = new TransactionScope())
                {
                    var textb = new StringBuilder();
                    for (int i = 0; i < 2000; i++) textb.Append("x");
                    var text = textb.ToString();
                    var count = db.Sql("select count(*) from Posts").AsScalar<int>();
                    Trace.WriteLine("count: " + count);

                    if (count > nofRecords)
                    {
                        db.Sql("delete from Posts where id > @p").WithParameter("p", count).AsNonQuery();
                    }
                    else
                        for (int i = count + 1; i <= nofRecords; i++)
                        {
                            db.Sql("insert into Posts values (@Id, @Text, @CreationDate, @LastChangeDate, " +
                                   "@Counter1, @Counter2, @Counter3, @Counter4, @Counter5, @Counter6, @Counter7, @Counter8, @Counter9)")
                                .WithParameters(new Post
                                {
                                    Id = i,
                                    CreationDate = DateTime.Now,
                                    LastChangeDate = DateTime.Now,
                                    Text = text
                                }).AsNonQuery();
                        }
                    tx.Complete();
                }
            }
        }

        private static void Run(int iterations, Func<IDb, PerformanceTests.Tests> testGenerator)
        {
            var performanceTests = new PerformanceTests();
            var results = performanceTests.Run(iterations, testGenerator).ToList();
            var max = results.Select(r => r.ElapsedMilliseconds).Max();
            var min = results.Select(r => r.ElapsedMilliseconds).Min();
            var difference = (decimal) min/max;
            if (difference < .333m)
                Assert.Fail("One of the scenarios performs more than 3 times more slowly!");
        }
    }

    class PerformanceTests
    {
        internal class Test
        {
            public static Test Create(Action<int> iteration, string name)
            {
                return new Test { Iteration = iteration, Name = name };
            }

            public Action<int> Iteration { get; set; }
            public string Name { get; set; }
            public Stopwatch Watch { get; set; }
        }

        internal class TestResult
        {
            private readonly string _name;
            private readonly long _elapsedMilliseconds;

            public TestResult(string name, long elapsedMilliseconds)
            {
                _name = name;
                _elapsedMilliseconds = elapsedMilliseconds;
            }

            public string Name
            {
                get { return _name; }
            }

            public long ElapsedMilliseconds
            {
                get { return _elapsedMilliseconds; }
            }
        }

        internal class Tests : List<Test>
        {
            public void Add(Action<int> iteration, string name)
            {
                Add(Test.Create(iteration, name));
            }

            public IEnumerable<TestResult> Run(int iterations)
            {
                // warmup 
                foreach (var test in this)
                {
                    test.Iteration(iterations + 1);
                    test.Watch = new Stopwatch();
                    test.Watch.Reset();
                }

                var rand = new Random();
                for (int i = 1; i <= iterations; i++)
                {
                    foreach (var test in this.OrderBy(ignore => rand.Next()))
                    {
                        test.Watch.Start();
                        test.Iteration(i);
                        test.Watch.Stop();
                    }
                }

                foreach (var test in this.OrderBy(t => t.Watch.ElapsedMilliseconds))
                {
                    Console.WriteLine(test.Name + " took " + test.Watch.ElapsedMilliseconds + "ms");
                    yield return new TestResult(test.Name, test.Watch.ElapsedMilliseconds);
                }
            }
        }

        public IEnumerable<TestResult> Run(int iterations, Func<IDb, Tests> createTests)
        {
            Logger.Log = null;
            using (var db = Db.FromConfig("sqlite"))
            {
                var tests = createTests(db);
                return tests.Run(iterations).ToList();
            }
        }

        public static Tests SelectAllRecordsTests(IDb db)
        {
            var connection = db.Connection;
            var tests = new Tests();

            var commandBuilder = db
                .Sql("select * from Posts");

            tests.Add(id =>
            {
                var p = commandBuilder
                    .AsEnumerable(d => new Post
                    {
                        Id = d.Id,
                        Text = d.Text,
                        CreationDate = d.CreationDate,
                        LastChangeDate = d.LastChangeDate,
                        Counter1 = d.Counter1,
                        Counter2 = d.Counter2,
                        Counter3 = d.Counter3,
                        Counter4 = d.Counter4,
                        Counter5 = d.Counter5,
                        Counter6 = d.Counter6,
                        Counter7 = d.Counter7,
                        Counter8 = d.Counter8,
                        Counter9 = d.Counter9
                    }).ToList();
            }, "Net.Code.AdoNet with mapping from dynamic to T");

            tests.Add(id =>
            {
                commandBuilder
                    .AsEnumerable<Post>().ToList();
            }, "Net.Code.AdoNet with mapping via reflection");


            tests.Add(id =>
            {
                using (var d = commandBuilder.AsReader())
                {
                    while (d.Read())
                    {
                        var post = new Post
                        {
                            Id = d.Get<int>(0),
                            Text = d.Get<string>(1),
                            CreationDate = d.Get<DateTime>(2),
                            LastChangeDate = d.Get<DateTime>(3),
                            Counter1 = d.Get<int?>(4),
                            Counter2 = d.Get<int?>(5),
                            Counter3 = d.Get<int?>(6),
                            Counter4 = d.Get<int?>(7),
                            Counter5 = d.Get<int?>(8),
                            Counter6 = d.Get<int?>(9),
                            Counter7 = d.Get<int?>(10),
                            Counter8 = d.Get<int?>(11),
                            Counter9 = d.Get<int?>(12)
                        };
                    }
                }
            }, "Net.Code.AdoNet datareader");


            // HAND CODED 

            var postCommand = connection.CreateCommand();
            postCommand.CommandText = @"select Id, [Text], [CreationDate], LastChangeDate, 
                Counter1,Counter2,Counter3,Counter4,Counter5,Counter6,Counter7,Counter8,Counter9 from Posts";
            tests.Add(id =>
            {
                using (var reader = postCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var post = new Post
                        {
                            Id = reader.GetInt32(0),
                            Text = ConvertTo<string>.From(reader.GetValue(1)),
                            CreationDate = reader.GetDateTime(2),
                            LastChangeDate = reader.GetDateTime(3),
                            Counter1 = reader.GetNullableValue<int>(4),
                            Counter2 = reader.GetNullableValue<int>(5),
                            Counter3 = reader.GetNullableValue<int>(6),
                            Counter4 = reader.GetNullableValue<int>(7),
                            Counter5 = reader.GetNullableValue<int>(8),
                            Counter6 = reader.GetNullableValue<int>(9),
                            Counter7 = reader.GetNullableValue<int>(10),
                            Counter8 = reader.GetNullableValue<int>(11),
                            Counter9 = reader.GetNullableValue<int>(12)
                        };
                    }
                }
            }, "hand coded");

            DataTable table = new DataTable
            {
                Columns =
                {
                    {"Id", typeof (int)},
                    {"Text", typeof (string)},
                    {"CreationDate", typeof (DateTime)},
                    {"LastChangeDate", typeof (DateTime)},
                    {"Counter1", typeof (int)},
                    {"Counter2", typeof (int)},
                    {"Counter3", typeof (int)},
                    {"Counter4", typeof (int)},
                    {"Counter5", typeof (int)},
                    {"Counter6", typeof (int)},
                    {"Counter7", typeof (int)},
                    {"Counter8", typeof (int)},
                    {"Counter9", typeof (int)},
                }
            };
            tests.Add(id =>
            {
                object[] values = new object[13];
                using (var reader = postCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reader.GetValues(values);
                        table.Rows.Add(values);
                    }
                }
            }, "DataTable via IDataReader.GetValues");

            tests.Add(id =>
            {
                commandBuilder.AsDataTable();
            }, "Net.Code.ADONet AsDataTable");

            return tests;
        }
        public static Tests SelectSingleRecordTests(IDb db)
        {
            var connection = db.Connection;
            var tests = new Tests();

            var commandBuilder = db
                .Sql("select * from Posts where Id = @id");

            tests.Add(id =>
            {
                var p = commandBuilder
                    .WithParameter("id", id)
                    .AsEnumerable(d => new Post
                    {
                        Id = d.Id,
                        Text = d.Text,
                        CreationDate = d.CreationDate,
                        LastChangeDate = d.LastChangeDate,
                        Counter1 = d.Counter1,
                        Counter2 = d.Counter2,
                        Counter3 = d.Counter3,
                        Counter4 = d.Counter4,
                        Counter5 = d.Counter5,
                        Counter6 = d.Counter6,
                        Counter7 = d.Counter7,
                        Counter8 = d.Counter8,
                        Counter9 = d.Counter9
                    }).First();
            }, "Net.Code.AdoNet with mapping from dynamic to T");

            tests.Add(id =>
            {
                commandBuilder
                    .WithParameter("id", id)
                    .AsEnumerable<Post>().ToList();
            }, "Net.Code.AdoNet with mapping via reflection");


            tests.Add(id =>
            {
                using (var d = commandBuilder.WithParameter("id", id).AsReader())
                {
                    while (d.Read())
                    {
                        var post = new Post
                        {
                            Id = d.Get<int>(0),
                            Text = d.Get<string>(1),
                            CreationDate = d.Get<DateTime>(2),
                            LastChangeDate = d.Get<DateTime>(3),
                            Counter1 = d.Get<int?>(4),
                            Counter2 = d.Get<int?>(5),
                            Counter3 = d.Get<int?>(6),
                            Counter4 = d.Get<int?>(7),
                            Counter5 = d.Get<int?>(8),
                            Counter6 = d.Get<int?>(9),
                            Counter7 = d.Get<int?>(10),
                            Counter8 = d.Get<int?>(11),
                            Counter9 = d.Get<int?>(12)
                        };
                    }
                }
            }, "Net.Code.AdoNet datareader");


            // HAND CODED 

            var postCommand = connection.CreateCommand();
            postCommand.CommandText = @"select Id, [Text], [CreationDate], LastChangeDate, 
                Counter1,Counter2,Counter3,Counter4,Counter5,Counter6,Counter7,Counter8,Counter9 from Posts where Id = @Id";
            var idParam = postCommand.CreateParameter();
            idParam.ParameterName = "Id";
            postCommand.Parameters.Add(idParam);
            tests.Add(id =>
            {
                idParam.Value = id;

                using (var reader = postCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var post = new Post
                        {
                            Id = reader.GetInt32(0),
                            Text = reader.GetNullableString(1),
                            CreationDate = reader.GetDateTime(2),
                            LastChangeDate = reader.GetDateTime(3),
                            Counter1 = reader.GetNullableValue<int>(4),
                            Counter2 = reader.GetNullableValue<int>(5),
                            Counter3 = reader.GetNullableValue<int>(6),
                            Counter4 = reader.GetNullableValue<int>(7),
                            Counter5 = reader.GetNullableValue<int>(8),
                            Counter6 = reader.GetNullableValue<int>(9),
                            Counter7 = reader.GetNullableValue<int>(10),
                            Counter8 = reader.GetNullableValue<int>(11),
                            Counter9 = reader.GetNullableValue<int>(12)
                        };
                    }
                }
            }, "hand coded");

            DataTable table = new DataTable
            {
                Columns =
                {
                    {"Id", typeof (int)},
                    {"Text", typeof (string)},
                    {"CreationDate", typeof (DateTime)},
                    {"LastChangeDate", typeof (DateTime)},
                    {"Counter1", typeof (int)},
                    {"Counter2", typeof (int)},
                    {"Counter3", typeof (int)},
                    {"Counter4", typeof (int)},
                    {"Counter5", typeof (int)},
                    {"Counter6", typeof (int)},
                    {"Counter7", typeof (int)},
                    {"Counter8", typeof (int)},
                    {"Counter9", typeof (int)},
                }
            };
            tests.Add(id =>
            {
                idParam.Value = id;
                object[] values = new object[13];
                using (var reader = postCommand.ExecuteReader())
                {
                    reader.Read();
                    reader.GetValues(values);
                    table.Rows.Add(values);
                }
            }, "DataTable via IDataReader.GetValues");

            //tests.Add(id =>
            //{
            //    commandBuilder.WithParameter("id", id).AsDataTable();
            //}, "Net.Code.ADONet AsDataTable");
            return tests;
        }
    }

    internal class Post
    {
        public decimal Id { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastChangeDate { get; set; }
        public int? Counter1 { get; set; }
        public int? Counter2 { get; set; }
        public int? Counter3 { get; set; }
        public int? Counter4 { get; set; }
        public int? Counter5 { get; set; }
        public int? Counter6 { get; set; }
        public int? Counter7 { get; set; }
        public int? Counter8 { get; set; }
        public int? Counter9 { get; set; }
    }

    static class IDataReaderHelper
    {
        public static string GetNullableString(this IDataReader reader, int index)
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (string)tmp;
            }
            return null;
        }

        public static Nullable<T> GetNullableValue<T>(this IDataReader reader, int index) where T : struct
        {
            object tmp = reader.GetValue(index);
            if (tmp != DBNull.Value)
            {
                return (T)tmp;
            }
            return null;
        }
    }
}