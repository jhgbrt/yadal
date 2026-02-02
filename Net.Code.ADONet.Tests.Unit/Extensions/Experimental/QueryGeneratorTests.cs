using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit.Extensions.Experimental
{
    class MyEntityWithGeneratedId
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    class MyEntity
    {
        public int MyEntityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    class MyEntityWithCompositeKey
    {
        [Key]
        public string Parent { get; set; }
        [Key]
        public string Name { get; set; }
        public string Description { get; set; }
    }


    public class QueryGeneratorTestsForEntityWithDatabaseGeneratedId
    {
        Query query = QueryFactory<MyEntityWithGeneratedId>.Get(MappingConvention.Default);

        [Fact]
        public void Insert()
        {
            var sql = query.Insert;
            Assert.Equal("INSERT INTO MyEntityWithGeneratedId (Name, Description) VALUES (@Name, @Description)", sql);
        }
        [Fact]
        public void Delete()
        {
            var sql = query.Delete;
            Assert.Equal("DELETE FROM MyEntityWithGeneratedId WHERE Id = @Id", sql);
        }
        [Fact]
        public void Update()
        {
            var sql = query.Update;
            Assert.Equal("UPDATE MyEntityWithGeneratedId SET Name = @Name, Description = @Description WHERE Id = @Id", sql);
        }
        [Fact]
        public void Select()
        {
            var sql = query.Select;
            Assert.Equal("SELECT Id, Name, Description FROM MyEntityWithGeneratedId WHERE Id = @Id", sql);
        }
        [Fact]
        public void SelectAll()
        {
            var sql = query.SelectAll;
            Assert.Equal("SELECT Id, Name, Description FROM MyEntityWithGeneratedId", sql);
        }
        [Fact]
        public void Count()
        {
            var sql = query.Count;
            Assert.Equal("SELECT COUNT(*) FROM MyEntityWithGeneratedId", sql);
        }
    }


    public class QueryGeneratorTestsForEntityOracleConvention
    {
        Query query = QueryFactory<MyEntity>.Get(DbConfig.FromProviderName("Oracle").MappingConvention);

        [Fact]
        public void Insert()
        {
            var sql = query.Insert;
            Assert.Equal("INSERT INTO MY_ENTITY (MY_ENTITY_ID, NAME, DESCRIPTION) VALUES (:MyEntityId, :Name, :Description)", sql);
        }
        [Fact]
        public void Delete()
        {
            var sql = query.Delete;
            Assert.Equal("DELETE FROM MY_ENTITY WHERE MY_ENTITY_ID = :MyEntityId", sql);
        }
        [Fact]
        public void Update()
        {
            var sql = query.Update;
            Assert.Equal("UPDATE MY_ENTITY SET NAME = :Name, DESCRIPTION = :Description WHERE MY_ENTITY_ID = :MyEntityId", sql);
        }
        [Fact]
        public void Select()
        {
            var sql = query.Select;
            Assert.Equal("SELECT MY_ENTITY_ID, NAME, DESCRIPTION FROM MY_ENTITY WHERE MY_ENTITY_ID = :MyEntityId", sql);
        }
        [Fact]
        public void SelectAll()
        {
            var sql = query.SelectAll;
            Assert.Equal("SELECT MY_ENTITY_ID, NAME, DESCRIPTION FROM MY_ENTITY", sql);
        }
        [Fact]
        public void Count()
        {
            var sql = query.Count;
            Assert.Equal("SELECT COUNT(*) FROM MY_ENTITY", sql);
        }
    }

    public class QueryGeneratorTestsForDefaultEntity
    {
        Query query = QueryFactory<MyEntity>.Get(DbConfig.FromProviderName("Npgsql").MappingConvention);

        [Fact]
        public void Insert()
        {
            var sql = query.Insert;
            Assert.Equal("INSERT INTO my_entity (my_entity_id, name, description) VALUES (@MyEntityId, @Name, @Description)", sql);
        }
        [Fact]
        public void Delete()
        {
            var sql = query.Delete;
            Assert.Equal("DELETE FROM my_entity WHERE my_entity_id = @MyEntityId", sql);
        }
        [Fact]
        public void Update()
        {
            var sql = query.Update;
            Assert.Equal("UPDATE my_entity SET name = @Name, description = @Description WHERE my_entity_id = @MyEntityId", sql);
        }
        [Fact]
        public void Select()
        {
            var sql = query.Select;
            Assert.Equal("SELECT my_entity_id, name, description FROM my_entity WHERE my_entity_id = @MyEntityId", sql);
        }
        [Fact]
        public void SelectAll()
        {
            var sql = query.SelectAll;
            Assert.Equal("SELECT my_entity_id, name, description FROM my_entity", sql);
        }
        [Fact]
        public void Count()
        {
            var sql = query.Count;
            Assert.Equal("SELECT COUNT(*) FROM my_entity", sql);
        }
    }

    public class QueryGeneratorTestsForEntityWithCompositeKey
    {
        Query query = QueryFactory<MyEntityWithCompositeKey>.Get(DbConfig.FromProviderName("Oracle").MappingConvention);

        [Fact]
        public void Insert()
        {
            var sql = query.Insert;
            Assert.Equal("INSERT INTO MY_ENTITY_WITH_COMPOSITE_KEY (PARENT, NAME, DESCRIPTION) VALUES (:Parent, :Name, :Description)", sql);
        }
        [Fact]
        public void Delete()
        {
            var sql = query.Delete;
            Assert.Equal("DELETE FROM MY_ENTITY_WITH_COMPOSITE_KEY WHERE PARENT = :Parent AND NAME = :Name", sql);
        }
        [Fact]
        public void Update()
        {
            var sql = query.Update;
            Assert.Equal("UPDATE MY_ENTITY_WITH_COMPOSITE_KEY SET DESCRIPTION = :Description WHERE PARENT = :Parent AND NAME = :Name", sql);
        }
        [Fact]
        public void Select()
        {
            var sql = query.Select;
            Assert.Equal("SELECT PARENT, NAME, DESCRIPTION FROM MY_ENTITY_WITH_COMPOSITE_KEY WHERE PARENT = :Parent AND NAME = :Name", sql);
        }
        [Fact]
        public void SelectAll()
        {
            var sql = query.SelectAll;
            Assert.Equal("SELECT PARENT, NAME, DESCRIPTION FROM MY_ENTITY_WITH_COMPOSITE_KEY", sql);
        }
        [Fact]
        public void Count()
        {
            var sql = query.Count;
            Assert.Equal("SELECT COUNT(*) FROM MY_ENTITY_WITH_COMPOSITE_KEY", sql);
        }
    }

    public class QueryFactoryToKeyTests
    {
        record Person(int Id, string Name);
        class PersonAddress
        {
            [Key] public int PersonId { get; set; }
            [Key] public int AddressId { get; set; }
        }

        [Fact]
        public void IntegerKeyValue()
        {
            var key = QueryFactory<Person>.ToKey(5);
            Assert.True(key.values.Equals(new[] { ("Id", (object)5) }));
        }
        [Fact]
        public void IntegerKeyPropertyValue()
        {
            var key = QueryFactory<Person>.ToKey(new { Id = 5 });
            Assert.True(key.values.Equals(new[] { ("Id", (object)5) }));
        }
        [Fact]
        public void CompositeKeyValue()
        {
            var key = QueryFactory<PersonAddress>.ToKey(new {PersonId = 5, AddressId = 6});
            Assert.True(key.values.Equals(new[] { ("PersonId", (object)5), ("AddressId", (object)6) }));
        }

    }

    public class ValueListTests()
    {
        ValueList<int?> list = [1, 2, null, 3];

        [Fact]
        public void Equals_SameTypeAndValues_Succeeds()
        {
            ValueList<int?> other = [1, 2, null, 3];
            Assert.True(list.Equals(other));
        }

        [Fact]
        public void Equals_SameTypeOtherValues_Fails()
        {
            ValueList<int?> other = [1, 2, null, 4];
            Assert.False(list.Equals(other));
        }

        [Fact]
        public void Equals_SameUnderlyingTypeAndValues_Succeeds()
        {
            List<int?> other = [1, 2, null, 3];
            Assert.True(list.Equals(other));
        }

        [Fact]
        public void Equals_SameUnderlyingTypeOtherValues_Fails()
        {
            List<int?> other = [1, 2, null, 4];
            Assert.False(list.Equals(other));
        }

        [Fact]
        public void Equals_SameUnderlyingTypeDefinedAsObjectAndSameValues_Succeeds()
        {
            List<object> other = [1, 2, null, 3];
            Assert.True(list.Equals(other));
        }

        [Fact]
        public void Equals_SameUnderlyingTypeDefinedAsObjectAndOtherValues_Fails()
        {
            List<object> other = [1, 2, null, 4];
            Assert.False(list.Equals(other));
        }
    }
}
