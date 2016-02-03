using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Net.Code.ADONet.Tests.SqlServer.FluentDates;

namespace Net.Code.ADONet.Tests.SqlServer
{

    public static class FluentDates
    {
        public static DateTime FirstDayOfMonth(this DateTime dateTime)
        {
            return dateTime.Date.AddDays(-dateTime.Day);
        }
        public static DateTime LastDayOfMonth(this DateTime dateTime)
        {
            return dateTime.Date.FirstDayOfMonth().AddMonths(1).AddDays(-1);
        }

        public static DateTime FirstDayOfWeek(this DateTime dateTime, CultureInfo cultureInfo = null)
        {
            cultureInfo = cultureInfo ?? CultureInfo.CurrentCulture;
            var firstDay = cultureInfo.DateTimeFormat.FirstDayOfWeek;
            var dayOfWeek = cultureInfo.Calendar.GetDayOfWeek(dateTime);
            return dateTime.Date.AddDays(firstDay - dayOfWeek);
        }

        public static DateTime NextWeek(this DateTime dateTime)
        {
            return dateTime.AddDays(7);
        }

        public struct M
        {
            private readonly int _year;
            private readonly int _month;

            public M(int year, int month)
            {
                _year = year;
                _month = month;
            }

            public DateTime First => new DateTime(_year, _month, 1);
            public DateTime Day(int d) => new DateTime(_year, _month, d);
        }

        public struct Y
        {
            private readonly int _year;
            public Y(int year)
            {
                _year = year;
            }
            public M January => new M(_year, 1);
            public M February => new M(_year, 2);
            public M March => new M(_year, 3);
            public M April => new M(_year, 4);
            public M May => new M(_year, 5);
            public M June => new M(_year, 6);
            public M July => new M(_year, 7);
            public M August => new M(_year, 8);
            public M September => new M(_year, 9);
            public M October => new M(_year, 10);
            public M November => new M(_year, 11);
            public M December => new M(_year, 12);
        }

        public static Y Year(int year)
        {
            return new Y(year);
        }
    }


    [TestClass]
    public class SqlServerIntegrationTests
    {
        private static readonly string ConnectionString = "Data Source=localhost;" +
                                                          "Initial Catalog=Test;" +
                                                          "Integrated Security=True";

        [TestInitialize]
        public void Setup()
        {
            var dropIfExists = "IF EXISTS (" +
                               "    SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Person'" +
                               ") " +
                               "    DROP TABLE Person;";

            ExecuteNonQuery(dropIfExists);

            var createTable =
                "CREATE TABLE Person (" +
                "   Id int identity not null," +
                "   FirstName nvarchar(10)," +
                "   LastName nvarchar(10)," +
                "   Email nvarchar(200)," +
                "   BirthDate datetime2" +
                ");";

            ExecuteNonQuery(createTable);
        }

        private static void ExecuteNonQuery(string query)
        {
            using (var db = new Db(ConnectionString))
            {
                db.Sql(query).AsNonQuery();
            }
        }

        private static void ExecuteNonQuery<T>(string query, T parameters)
        {
            using (var db = new Db(ConnectionString))
            {
                db.Sql(query).WithParameters(parameters).AsNonQuery();
            }
        }
        private static IList<T> AsEnumerable<T>(string query, Func<dynamic, T> selector)
        {
            using (var db = new Db(ConnectionString))
            {
                return db.Sql(query).AsEnumerable(selector).ToList();
            }
        }
        private static List<List<dynamic>> AsMultiResultSet(string query)
        {
            using (var db = new Db(ConnectionString))
            {
                return db.Sql(query).AsMultiResultSet().Select(x => x.ToList()).ToList();
            }
        }

        [TestMethod]
        public void CanCreateAndDropTable()
        {

            var insert =
                "INSERT INTO Person (" +
                "   FirstName, LastName, Email, BirthDate" +
                ") VALUES (" +
                "   @FirstName, @LastName, @Email, @BirthDate" +
                ")";


            var birthDate = Year(1970).January.First;

            ExecuteNonQuery(insert, new { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", BirthDate = birthDate });

            var people = AsEnumerable("SELECT TOP 1 * FROM Person", d => (Person)Person.From(d));

            var person = people[0];

            Assert.AreEqual("John", person.FirstName);
            Assert.AreEqual("Doe", person.LastName);
            Assert.AreEqual("john.doe@example.com", person.Email);
            Assert.AreEqual(birthDate, person.BirthDate);


            var multiResultSet = AsMultiResultSet("SELECT TOP 1 Id, FirstName FROM Person;" +
                                                  "SELECT TOP 1 Id, BirthDate FROM Person");

            var set1 = multiResultSet[0];
            var set2 = multiResultSet[1];

            Assert.AreEqual("John", set1.First().FirstName);
            Assert.AreEqual(birthDate, set2.First().BirthDate);
        }


        [TestCleanup]
        public void Cleanup()
        {
            var dropIfExists = "IF EXISTS (" +
                               "    SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Person'" +
                               ") " +
                               "    DROP TABLE Person;";

            ExecuteNonQuery(dropIfExists);
        }
    }

    internal class Person
    {
        public string Email { get; }
        public DateTime BirthDate { get; }
        public int Id { get; }
        public string FirstName { get; }
        public string LastName { get; }

        public Person(int id, string firstName, string lastName, string email, DateTime birthDate)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            BirthDate = birthDate;
        }

        public static Person From(dynamic d)
        {
            return new Person(d.Id, d.FirstName, d.LastName, d.Email, d.BirthDate);
        }
    }
}
