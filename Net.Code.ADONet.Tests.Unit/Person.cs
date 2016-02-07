using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;

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
            Assert.AreEqual("FirstName", result.Single().FirstName);
        }
        public static void VerifyDataTable(DataTable result)
        {
            Assert.AreEqual("FirstName", result.Rows[0]["FirstName"]);
        }

        public static void VerifyMultiResultSet(List<List<dynamic>> result)
        {
            Assert.AreEqual("FirstPersonOfFirstList", result[0][0].FirstName);
            Assert.AreEqual("FirstPersonOfSecondList", result[1][0].FirstName);
            Assert.AreEqual("SecondPersonOfSecondList", result[1][1].FirstName);
        }

        public static void VerifyResult(Person result)
        {
            Assert.AreEqual("FirstName", result.FirstName);
        }
    }
}