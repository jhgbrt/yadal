using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit
{
    [TestFixture]
    public class CommandExecutionTests
    {
        // can not get the ListDataReader to work for this case
        [Test] 
        public void AsDatatable_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsDataTable();

            Person.VerifyDataTable(result);
        }

        [Test]
        public void AsEnumerable_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable().ToList();

            Person.VerifySingleResultSet(result);
        }
        [Test]
        public void Single_WhenCalled_ReturnsSingleItem()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.Single<Person>();

            Person.VerifyResult(result);
        }

        [Test]
        public void AsEnumerableWithSelector_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable(d => new Person {FirstName = d.FirstName}).ToList();

            Person.VerifySingleResultSet(result);
        }
        [Test]
        public void AsEnumerableGeneric_WhenCalled_ReturnsResults()
        {
            var command = PrepareCommand();

            var commandBuilder = new CommandBuilder(command);

            command.SetResultSet(Person.GetSingleResultSet());

            var result = commandBuilder.AsEnumerable<Person>().ToList();

            Person.VerifySingleResultSet(result);
        }

        [Test]
        public void AsScalar_WhenCalled_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);

            var commandBuilder = new CommandBuilder(command);

            var result = commandBuilder.AsScalar<int>();

            Assert.AreEqual(1, result);
        }

        [Test]
        public void AsNonQuery_WhenCalled_ReturnsNonQueryResult()
        {
            var command = PrepareCommand();
            command.SetNonQueryResult(1);
            var commandBuilder = new CommandBuilder(command);

            var result = commandBuilder.AsNonQuery();
            
            Assert.AreEqual(1, result);
        }

        [Test]
        public void AsMultiResultSet_WhenCalled_ReturnsMultipleResultSets()
        {
            var command = PrepareCommand();

            var data = Person.GetMultiResultSet();

            command.SetMultiResultSet(data);
            var commandBuilder = new CommandBuilder(command);
            
            var result = commandBuilder.AsMultiResultSet().Select(x => x.ToList()).ToList();

            Person.VerifyMultiResultSet(result);
        }

        [Test]
        public async Task AsEnumerableAsync_WhenCalledAndAwaited_ReturnsResultSet()
        {
            var command = PrepareCommand();
            var commandBuilder = new CommandBuilder(command);
            command.SetResultSet(Person.GetSingleResultSet());

            var result = (await commandBuilder.AsEnumerableAsync()).ToList();

            Person.VerifySingleResultSet(result);
        }

        [Test]
        public async Task AsEnumerableAsyncWithSelector_WhenCalledAndAwaited_ReturnsResultSet()
        {
            var command = PrepareCommand();
            var commandBuilder = new CommandBuilder(command);
            command.SetResultSet(Person.GetSingleResultSet());

            var result = (await commandBuilder.AsEnumerableAsync(d => new Person{FirstName = d.FirstName})).ToList();

            Person.VerifySingleResultSet(result);
        }

        [Test]
        public async Task AsScalarAsync_WhenCalledAndAwaited_ReturnsScalarValue()
        {
            var command = PrepareCommand();
            command.SetScalarValue(1);
            var commandBuilder = new CommandBuilder(command);

            var result = await commandBuilder.AsScalarAsync<int>();

            Assert.AreEqual(1, result);
        }

        [Test]
        public async Task AsNonQueryAsync_WhenCalledAndAwaited_ReturnsNonQueryValue()
        {
            var command = PrepareCommand();
            command.SetNonQueryResult(1);
            var commandBuilder = new CommandBuilder(command);
            
            var result = await commandBuilder.AsNonQueryAsync();
            
            Assert.AreEqual(1, result);
        }

        [Test]
        public async Task AsMultipleResultSetAsync_WhenCalledAndAwaited_ReturnsMultiResultSet()
        {
            var command = PrepareCommand();
            command.SetMultiResultSet(Person.GetMultiResultSet());
            var commandBuilder = new CommandBuilder(command);
            
            var result = (await commandBuilder.AsMultiResultSetAsync()).Select(x => x.ToList()).ToList();

            Person.VerifyMultiResultSet(result);
        }

        private static FakeCommand PrepareCommand()
        {
            var command = new FakeCommand(new FakeConnection());
            return command;
        }
    }
}