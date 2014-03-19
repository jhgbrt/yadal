using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
namespace Net.Code.ADONet.Tests.Integration
{
    class MyRequiredAttribute : RequiredAttribute
    {
        public override bool IsValid(object value)
        {
            return value != null;
        }
    }
    class MyTable
    {
        public int Id { get; set; }
        [MyRequired]
        public string StringNotNull { get; set; }
        public string StringNull { get; set; }
        public int IntNotNull { get; set; }
        public int? IntNull { get; set; }
        public Guid? GuidNull { get; set; }
    }
    class MyDbContext : DbContext
    {
        public DbSet<MyTable> MyTable { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }

    class Initializer : DropCreateDatabaseAlways<MyDbContext>
    {
        protected override void Seed(MyDbContext context)
        {
            base.Seed(context);
            var entities = new[]
                               {
                                   new MyTable
                                       {
                                           StringNotNull = "",
                                           StringNull = string.Empty,
                                           IntNotNull = 0,
                                           IntNull = 0
                                       },
                                   new MyTable
                                       {
                                           StringNotNull = "",
                                           StringNull = null,
                                           IntNotNull = 0,
                                           IntNull = null
                                       }
                               };
            foreach (var e in entities) context.MyTable.Add(e);
            try
            {
                context.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                var messages = from err in e.EntityValidationErrors
                               from ve in err.ValidationErrors
                               select ve.ErrorMessage;
                foreach (var m in messages)
                {
                    Console.WriteLine(m);
                }
            }
        }
    }

    [TestClass]
    public abstract class given_an_initialized_database_with_one_table_and_two_records
    {
        protected static IDb db;

        [TestInitialize]
        public void EstablishContext()
        {
            Database.SetInitializer(new Initializer());

            string connectionString;

            using (var ctx = new MyDbContext())
            {
                connectionString = ctx.Database.Connection.ConnectionString;
                ctx.Database.Initialize(false);
            }

            db = new Db(connectionString, "System.Data.SqlServerCe.4.0");
            db.Configure().SetAsyncAdapter(new SqlCeAsyncAdapter());
            Given();
            When();
        }

        protected virtual void Given() { }
        protected virtual void When() { }
    }


    public class SqlCeAsyncAdapter : IAsyncAdapter
    {    
        public async Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            var sqlCeCommand = (SqlCeCommand)command;
            var result = await sqlCeCommand.ExecuteNonQueryAsync();
            return result;
        }

        public async Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            var sqlCeCommand = (SqlCeCommand)command;
            var result = await sqlCeCommand.ExecuteScalarAsync();
            return result;
        }

        public async Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            var sqlCeCommand = (SqlCeCommand)command;
            var result = await sqlCeCommand.ExecuteReaderAsync();
            return result;
        }

        public async Task OpenConnectionAsync(IDbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                var sqlConnection = (SqlCeConnection)connection;
                await sqlConnection.OpenAsync();
            }
        }
    }


    [TestClass]
    public class when_querying_as_dynamic : given_an_initialized_database_with_one_table_and_two_records
    {
        protected static IList<dynamic> result;

        protected override void Given()
        {
            base.Given();
            result = SelectAll();
        }

        protected override void When()
        {
        }

        [TestMethod]
        public void ItShouldNotBeEmpty()
        {
            Assert.IsTrue(result.Any());
        }

        [TestMethod]
        public void ItShouldNotBeEmptyAsync()
        {
            db.Configure().SetAsyncAdapter(new SqlCeAsyncAdapter());
            var result2 = SelectAllAsync().Result;
            Assert.IsTrue(result2.Any());
        }

        [TestMethod]
        public void It_should_contain_two_records()
        {
            Assert.AreEqual(2, result.Count());
        }

        protected async static Task<IList<dynamic>> SelectAllAsync()
        {
            var objects = await db.Sql("SELECT * FROM MyTable").AsEnumerableAsync();
            return objects.ToList();
        }

        protected static IList<dynamic> SelectAll()
        {
            return db.Sql("SELECT * FROM MyTable").AsEnumerable().ToList();
        }
    }

    [TestClass]
    public class when_retrieving_a_record_which_has_null_values : when_querying_as_dynamic
    {

        protected override void When()
        {
            base.When();
            record = result.Last();
        }

        [TestMethod]
        public void It_should_have_empty_string_for_nonnull_string()
        {
            string stringNotNull = record.StringNotNull;
            Assert.AreEqual(string.Empty, stringNotNull);
        }
        [TestMethod]
        public void It_should_have_null_string_for_null_string()
        {
            string stringNull = record.StringNull;
            Assert.IsNull(stringNull);
        }

        [TestMethod]
        public void It_should_have_0_for_nonnull_int()
        {
            int intNotNull = record.IntNotNull;
            Assert.AreEqual(0, intNotNull);
        }

        [TestMethod]
        public void It_should_have_null_for_null_int()
        {
            int? intNull = record.IntNull;
            Assert.IsNull(intNull);
        }
        private static dynamic record;
    }

    [TestClass]
    public class first_record_null_values : when_querying_as_dynamic
    {
        protected override void When()
        {
            base.When();
            record = result.First();
        }

        [TestMethod]
        public void It_should_have_empty_string_for_nonnull_string()
        {
            Assert.AreEqual(string.Empty, record.StringNotNull);
        }

        [TestMethod]
        public void It_should_have_empty_string_for_null_string()
        {
            Assert.AreEqual("", record.StringNull);
        }

        [TestMethod]
        public void It_should_have_0_for_nonnull_int()
        {
            Assert.AreEqual(0, record.IntNotNull);
        }

        [TestMethod]
        public void It_should_have_0_for_null_int()
        {
            Assert.AreEqual(0, record.IntNull);
        }

        private static dynamic record;
    }


    [TestClass]
    public class when_retrieving_scalar_value_for_integer_column_that_is_null : given_an_initialized_database_with_one_table_and_two_records
    {
        private static int? result;

        protected override void When()
        {
            base.When();
            result = db.Sql("SELECT TOP 1 IntNull FROM MyTable WHERE IntNull is null").AsScalar<int?>();
        }

        [TestMethod]
        public void It_should_be_null()
        {
            Assert.IsFalse(result.HasValue);
        }
    }

    [TestClass]
    public class when_retrieving_scalar_value_for_integer_column_that_is_null_async : given_an_initialized_database_with_one_table_and_two_records
    {
        private static int? result;

        protected override void When()
        {
            base.When();
            result = db.Sql("SELECT TOP 1 IntNull FROM MyTable WHERE IntNull is null").AsScalarAsync<int?>().Result;
        }

        [TestMethod]
        public void It_should_be_null()
        {
            Assert.IsFalse(result.HasValue);
        }
    }

    [TestClass]
    public class when_adding_row_with_guid : given_an_initialized_database_with_one_table_and_two_records
    {
        private static Guid expected = Guid.NewGuid();

        protected override void When()
        {
            base.When();
            db
                .Sql("INSERT INTO MyTable (StringNull, StringNotNull, IntNotNull, IntNull, GuidNull) VALUES (null, '', 0, null, @GuidValue)")
                .WithParameters(new { GuidValue = expected })
                .AsNonQuery();
        }

        [TestMethod]
        public void It_should_be_filled_in()
        {
            var result = db.Sql("SELECT TOP 1 GuidNull FROM MyTable WHERE GuidNull is not null").AsScalar<Guid>();
            Assert.AreEqual(expected, result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            db.Sql("DELETE FROM MyTable WHERE GuidNull is not null").AsNonQuery();
        }
    }
}
