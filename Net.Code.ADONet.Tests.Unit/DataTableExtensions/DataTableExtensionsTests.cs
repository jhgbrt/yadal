using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit.DataTableExtensions
{
    [TestFixture]
    public class DataTableExtensionsTests
    {
        private DataTable dt;

        [TestFixtureSetUp]
        public void Setup()
        {
            dt = new DataTable();
            dt.Columns.Add("Id", typeof (int));
            dt.Columns.Add("Description", typeof (string));
            dt.Rows.Add(1, "Description 1");
        }

        [Test]
        public void AsEnumerable()
        {
            var item = dt.AsEnumerable().Select(d => new { d.Id, d.Description }).First();
            Assert.AreEqual("Description 1", item.Description);
            Assert.AreEqual(1, item.Id);
        }

        [Test]
        public void LinqSelect()
        {
            var query = from d in dt
                select new { d.Id, d.Description };
            var item = query.First();
            Assert.AreEqual("Description 1", item.Description);
            Assert.AreEqual(1, item.Id);
        }
 
        [Test]
        public void LinqWhere()
        {
            var query = from d in dt
                        where d.Id == 1
                        select new { d.Id, d.Description };
            var item = query.First();
            Assert.AreEqual("Description 1", item.Description);
            Assert.AreEqual(1, item.Id);
        }
    }

    [TestFixture]
    public class ToDataTableTests
    {
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Guid UniqueID { get; set; }
            public string Description { get; set; }
            public string Address { get; set; }
        }

        IEnumerable<Person> People
        {
            get { return Enumerable.Range(0, 10000).Select(i => new Person
                                                               {
                                                                   Id = 1,
                                                                   Name = "Person " + i,
                                                                   Description = "Description of person " + i,
                                                                   Address = "Address of person " + i,
                                                                   UniqueID = Guid.NewGuid()
                                                               }); }
        }

        [Test]
        public void ToDataTable()
        {
            var sw = Stopwatch.StartNew();
            People.ToDataTable();
            Console.WriteLine(sw.Elapsed);

            sw = Stopwatch.StartNew();
            People.ToDataTable();
            Console.WriteLine(sw.Elapsed);

        }
    }
}
