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
        Query query = QueryFactory<MyEntityWithGeneratedId>.Create(MappingConvention.Default);

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
        Query query = QueryFactory<MyEntity>.Create(DbConfig.FromProviderName("Oracle").MappingConvention);

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
        Query query = QueryFactory<MyEntity>.Create(DbConfig.FromProviderName("Npgsql").MappingConvention);

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
        Query query = QueryFactory<MyEntityWithCompositeKey>.Create(DbConfig.FromProviderName("Oracle").MappingConvention);

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
}
