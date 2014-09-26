using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit
{
    class Person
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string FirstName { get; set; }

        public static Person[][] GetMultiResultSet()
        {
            var data = new[]
                       {
                           new[]
                           {
                               new Person {FirstName = "FirstPersonOfFirstList"}
                           },
                           new[]
                           {
                               new Person {FirstName = "FirstPersonOfSecondList"},
                               new Person {FirstName = "SecondPersonOfSecondList"}
                           }
                       };
            return data;
        }

        // ReSharper restore UnusedAutoPropertyAccessor.Local
        public static Person[] GetSingleResultSet()
        {
            var data = new[] { new Person { FirstName = "FirstName" } };
            return data;
        }

        public static void VerifySingleResultSet(IEnumerable<dynamic> result)
        {
            Assert.AreEqual("FirstName", result.Single().FirstName);
        }

        public static void VerifyMultiResultSet(List<List<dynamic>> result)
        {
            Assert.AreEqual("FirstPersonOfFirstList", result[0][0].FirstName);
            Assert.AreEqual("FirstPersonOfSecondList", result[1][0].FirstName);
            Assert.AreEqual("SecondPersonOfSecondList", result[1][1].FirstName);
        }
    }
}