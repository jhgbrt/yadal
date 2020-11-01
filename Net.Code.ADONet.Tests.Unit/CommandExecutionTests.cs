using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Net.Code.ADONet.Tests.Unit
{
    public class CommandExecutionTests
    {
        [Fact] 
        public void AsDatatable_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsDataTable();

            Person.VerifyDataTable(result);
        }

        [Fact]
        public void AsEnumerable_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable().ToList();

            Person.VerifySingleResultSet(result);
        }
        [Fact]
        public void Single_WhenCalled_ReturnsSingleItem()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.Single<Person>();

            Person.VerifyResult(result);
        }
        [Fact]
        public async Task SingleAsync_WhenCalled_ReturnsSingleItem()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = await commandBuilder.SingleAsync<Person>();

            Person.VerifyResult(result);
        }

        [Fact]
        public void AsEnumerableWithSelector_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable(d => (Person)Person.From(d)).ToList();

            Person.VerifySingleResultSet(result);
        }
        [Fact]
        public void AsEnumerableGeneric_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable<Person>().ToList();

            Person.VerifySingleResultSet(result);
        }

        [Fact]
        public void AsScalar_WhenCalled_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsScalar<int>();

            Assert.Equal(1, result);
        }

        [Fact]
        public void AsScalarObject_WhenCalled_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsScalar<object>();

            Assert.Equal(1, result);
        }

        [Fact]
        public void AsNonQuery_WhenCalled_ReturnsNonQueryResult()
        {
            var command = PrepareCommand();
            command.SetNonQueryResult(1);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsNonQuery();
            
            Assert.Equal(1, result);
        }

        [Fact]
        public void AsMultiResultSet_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            
            var result = commandBuilder.AsMultiResultSet().ToList();

            Person.VerifyMultiResultSet(result);
        }

        [Fact]
        public void AsMultiResultSetGeneric_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person>();

            Person.VerifyMultiResultSet(result.Item1, result.Item2);
        }
        [Fact]
        public void AsMultiResultSetGeneric3_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person, Person>();

            Person.VerifyMultiResultSet(result.Item1, result.Item2, result.Item3);
        }
        [Fact]
        public void AsMultiResultSetGeneric4_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person, Person, Person>();

            Person.VerifyMultiResultSet(result.Item1, result.Item2, result.Item3, result.Item4);
        }
        [Fact]
        public void AsMultiResultSetGeneric5_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person, Person, Person, Person>();

            Person.VerifyMultiResultSet(result.Item1, result.Item2, result.Item3, result.Item4, result.Item5);
        }

        [Fact]
        public async Task AsEnumerableAsync_WhenCalledAndAwaited_ReturnsResultSet()
        {
            var command = PrepareCommand();
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            command.SetResultSet(Person.GetSingleResultSet());

            var list = new List<dynamic>();
            await foreach (var item in commandBuilder.AsEnumerableAsync())
                list.Add(item);

            Person.VerifySingleResultSet(list);
        }

        [Fact]
        public async Task AsEnumerableAsyncWithSelector_WhenCalledAndAwaited_ReturnsResultSet()
        {
            var command = PrepareCommand();
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            command.SetResultSet(Person.GetSingleResultSet());

            var result = new List<Person>();
            await foreach (var p in commandBuilder.AsEnumerableAsync(d => (Person)Person.From(d)))
                result.Add(p);

            Person.VerifySingleResultSet(result);
        }
        [Fact]
        public async Task AsEnumerableAsyncForType_WhenCalledAndAwaited_ReturnsResultSet()
        {
            var command = PrepareCommand();
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            command.SetResultSet(Person.GetSingleResultSet());

            var result = new List<Person>();
            await foreach (var p in commandBuilder.AsEnumerableAsync<Person>())
                result.Add(p);

            Person.VerifySingleResultSet(result);
        }

        [Fact]
        public async Task AsScalarAsync_WhenCalledAndAwaited_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = await commandBuilder.AsScalarAsync<int>();

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task AsNonQueryAsync_WhenCalledAndAwaited_ReturnsNonQueryValue()
        {
            var command = PrepareCommand();
            command.SetNonQueryResult(1);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            
            var result = await commandBuilder.AsNonQueryAsync();
            
            Assert.Equal(1, result);
        }

        private static FakeCommand PrepareCommand()
        {
            var command = new FakeCommand(new FakeConnection());
            return command;
        }
    }
}