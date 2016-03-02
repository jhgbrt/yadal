using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit
{
    class Person
    {
        public int Id { get; set; }
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public static Person[][] GetMultiResultSet()
        {
            var data = new[]
                       {
                           new[]
                           {
                               new Person {Id = 1, FirstName = "FirstPersonOfFirstList", LastName = "Janssens"}
                           },
                           new[]
                           {
                               new Person {Id = 2, FirstName = "FirstPersonOfSecondList", LastName = "Janssens"},
                               new Person {Id = 3, FirstName = "SecondPersonOfSecondList", LastName = "Peeters"}
                           },
                           new[]
                           {
                               new Person {Id = 4, FirstName = "FirstPersonOfThirdList", LastName = "Janssens"},
                               new Person {Id = 5, FirstName = "SecondPersonOfThirdList", LastName = "Peeters"}
                           }
                       };
            return data;
        }

        // ReSharper restore UnusedAutoPropertyAccessor.Local
        public static Person[] GetSingleResultSet()
        {
            var data = new[] { new Person { Id = 1, FirstName = "FirstName", LastName = "Janssens" } };
            return data;
        }

        public static void VerifySingleResultSet(IEnumerable<dynamic> result)
        {
            Assert.AreEqual(1, result.Single().Id);
        }
        public static void VerifyDataTable(DataTable result)
        {
            Assert.AreEqual(1, result.Rows[0]["Id"]);
        }

        public static void VerifyMultiResultSet(List<IReadOnlyCollection<dynamic>> result)
        {
            var all = from list in result
                from item in list
                select (int) item.Id;
            VerifyIds(all);
        }

        private static void VerifyIds(IEnumerable<int> all)
        {
            var expected = 1;
            foreach (var id in all)
            {
                Assert.AreEqual(expected, id);
                expected++;
            }
        }

        public static void VerifyResult(Person result)
        {
            VerifyIds(new[] {result.Id});
        }

        public static void VerifyMultiResultSet(MultiResultSet<Person,Person> result)
        {
            var lists = new[]
            {
                result.Set1,
                result.Set2
            };
            
            VerifyIds(from list in lists from item in list select item.Id);
        }

        public static void VerifyMultiResultSet(MultiResultSet<Person, Person, Person> result)
        {
            var lists = new[]
            {
                result.Set1,
                result.Set2,
                result.Set3
            };
            VerifyIds(from list in lists from item in list select item.Id);
        }
        public static void VerifyMultiResultSet(MultiResultSet<Person, Person, Person, Person> result)
        {
            var lists = new[]
            {
                result.Set1,
                result.Set2,
                result.Set3,
                result.Set4,
            };
            VerifyIds(from list in lists from item in list select item.Id);
        }
        public static void VerifyMultiResultSet(MultiResultSet<Person, Person, Person, Person, Person> result)
        {
            var lists = new[]
            {
                result.Set1,
                result.Set2,
                result.Set3,
                result.Set4,
                result.Set5
            };
            VerifyIds(from list in lists from item in list select item.Id);
        }

        public static Person From(object o)
        {
            dynamic d = o;
            return new Person {Id = d.Id, FirstName = d.FirstName, LastName = d.LastName};
        }
    }
}