using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Linq;
using Machine.Specifications;

// ReSharper disable UnusedMember.Local
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

    public class given_an_initialized_database_with_one_table_and_two_records 
    {
        protected static IDb db;
        Establish context = () =>
                                {
                                    Database.SetInitializer(new Initializer());

                                    string connectionString;
                                    
                                    using (var ctx = new MyDbContext())
                                    {
                                        connectionString = ctx.Database.Connection.ConnectionString;
                                        ctx.Database.Initialize(false);
                                    }

                                    db = new Db(connectionString, "System.Data.SqlServerCe.4.0");
                                };

    }

    [Subject("when querying a database table as an enumerable of dynamic objects")]
    public class when_querying_as_dynamic : given_an_initialized_database_with_one_table_and_two_records
    {
        protected static IList<dynamic> result;

        Establish context = () => result = SelectAll();

        It should_not_be_empty = () => result.ShouldNotBeEmpty();

        It should_contain_two_records = () => result.Count().ShouldEqual(2);
        
        protected static IList<dynamic> SelectAll()
        {
            return db.Sql("SELECT * FROM MyTable").AsEnumerable().ToList();
        }
    }

    [Subject("querying dynamic record with null values")]
    public class when_retrieving_a_record_which_has_null_values : when_querying_as_dynamic
    {
        private Because of = () => record = result.Last();

        private It should_have_empty_string_for_nonnull_string = () => record.StringNotNull.ShouldEqual("");
        private It should_have_null_string_for_null_string = () => record.StringNull.ShouldBeNull();
        private It should_have_0_for_nonnull_int = () => record.IntNotNull.ShouldEqual(0);
        private It should_have_null_for_null_int = () => record.IntNull.ShouldBeNull();

        private static dynamic record;
    }

    [Subject("querying dynamic record without null values")]
    public class first_record_null_values : when_querying_as_dynamic
    {
        private Because of = () => record = result.First();

        private It should_have_empty_string_for_nonnull_string = () => ShouldExtensionMethods.ShouldEqual(record.StringNotNull, "");
        private It should_have_empty_string_for_null_string = () => ShouldExtensionMethods.ShouldEqual(record.StringNull, "");
        private It should_have_0_for_nonnull_int = () => ShouldExtensionMethods.ShouldEqual(record.IntNotNull, 0);
        private It should_have_0_for_null_int = () => ShouldExtensionMethods.ShouldEqual(record.IntNull, 0);

        private static dynamic record;
    }


    [Subject("when retrieving a null value from an integer db column that is null")]
    public class when_retrieving_scalar_value_for_integer_column_that_is_null : given_an_initialized_database_with_one_table_and_two_records
    {
        private static int? result;

        Because of = () => result = db.Sql("SELECT TOP 1 IntNull FROM MyTable WHERE IntNull is null").AsScalar<int?>();

        It should_be_null = () => result.HasValue.ShouldBeFalse();
    }

    [Subject("adding row with guid")]
    public class when_adding_row_with_guid : given_an_initialized_database_with_one_table_and_two_records
    {
        private static Guid expected = Guid.NewGuid();

        private Because of =
            () => db
                .Sql("INSERT INTO MyTable (StringNull, StringNotNull, IntNotNull, IntNull, GuidNull) VALUES (null, '', 0, null, @GuidValue)")
                .WithParameters(new {GuidValue = expected})
                .AsNonQuery();

        private It should_be_filled_in =
            () => db.Sql("SELECT TOP 1 GuidNull FROM MyTable WHERE GuidNull is not null").AsScalar<Guid>().ShouldEqual(expected);

        private Cleanup after = () => db.Sql("DELETE FROM MyTable WHERE GuidNull is not null").AsNonQuery();
    }
}
