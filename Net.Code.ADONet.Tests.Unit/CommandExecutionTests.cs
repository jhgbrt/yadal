using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit
{
    [TestClass]
    public class CommandExecutionTests
    {
        [TestMethod] 
        public void AsDatatable_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsDataTable();

            Person.VerifyDataTable(result);
        }

        [TestMethod]
        public void AsEnumerable_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable().ToList();

            Person.VerifySingleResultSet(result);
        }
        [TestMethod]
        public void Single_WhenCalled_ReturnsSingleItem()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.Single<Person>();

            Person.VerifyResult(result);
        }

        [TestMethod]
        public void AsEnumerableWithSelector_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable(d => (Person)Person.From(d)).ToList();

            Person.VerifySingleResultSet(result);
        }
        [TestMethod]
        public void AsEnumerableGeneric_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable<Person>().ToList();

            Person.VerifySingleResultSet(result);
        }

        [TestMethod]
        public void AsScalar_WhenCalled_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsScalar<int>();

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void AsScalarObject_WhenCalled_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);

            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsScalar<object>();

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void AsNonQuery_WhenCalled_ReturnsNonQueryResult()
        {
            var command = PrepareCommand();
            command.SetNonQueryResult(1);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsNonQuery();
            
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void AsMultiResultSet_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            
            var result = commandBuilder.AsMultiResultSet().ToList();

            Person.VerifyMultiResultSet(result);
        }

        [TestMethod]
        public void AsMultiResultSetGeneric_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person>();

            Person.VerifyMultiResultSet(result);
        }
        [TestMethod]
        public void AsMultiResultSetGeneric3_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person, Person>();

            Person.VerifyMultiResultSet(result);
        }
        [TestMethod]
        public void AsMultiResultSetGeneric4_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person, Person, Person>();

            Person.VerifyMultiResultSet(result);
        }
        [TestMethod]
        public void AsMultiResultSetGeneric5_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = commandBuilder.AsMultiResultSet<Person, Person, Person, Person, Person>();

            Person.VerifyMultiResultSet(result);
        }

        [TestMethod]
        public async Task AsEnumerableAsync_WhenCalledAndAwaited_ReturnsResultSet()
        {
            var command = PrepareCommand();
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            command.SetResultSet(Person.GetSingleResultSet());

            var result = (await commandBuilder.AsEnumerableAsync()).ToList();

            Person.VerifySingleResultSet(result);
        }

        [TestMethod]
        public async Task AsEnumerableAsyncWithSelector_WhenCalledAndAwaited_ReturnsResultSet()
        {
            var command = PrepareCommand();
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            command.SetResultSet(Person.GetSingleResultSet());

            var result = (await commandBuilder.AsEnumerableAsync(d => (Person)Person.From(d))).ToList();

            Person.VerifySingleResultSet(result);
        }

        [TestMethod]
        public async Task AsScalarAsync_WhenCalledAndAwaited_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);

            var result = await commandBuilder.AsScalarAsync<int>();

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public async Task AsNonQueryAsync_WhenCalledAndAwaited_ReturnsNonQueryValue()
        {
            var command = PrepareCommand();
            command.SetNonQueryResult(1);
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            
            var result = await commandBuilder.AsNonQueryAsync();
            
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public async Task AsMultipleResultSetAsync_WhenCalledAndAwaited_ReturnsMultiResultSet()
        {
            var command = PrepareCommand();
            command.SetMultiResultSet(Person.GetMultiResultSet());
            var commandBuilder = new CommandBuilder(command, DbConfig.Default);
            
            var result = (await commandBuilder.AsMultiResultSetAsync()).ToList();

            Person.VerifyMultiResultSet(result);
        }

        private static FakeCommand PrepareCommand()
        {
            var command = new FakeCommand(new FakeConnection());
            return command;
        }
    }
}