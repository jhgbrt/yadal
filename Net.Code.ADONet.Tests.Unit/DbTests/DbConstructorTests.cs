using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

using NSubstitute;
using Xunit;


namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    public class DbTests
    {
        [Fact]
        public void Connect_WhenCalledOnClosedConnection_OpensConnection()
        {
            var connection = Substitute.For<DbConnection>();
            connection.State.Returns(ConnectionState.Closed);
            var db = new Db(connection, DbConfig.Default);
            db.Connect();
            connection.Received(1).Open();
        }
        [Fact]
        public void Connect_WhenCalledOnOpenConnection_DoesNothing()
        {
            var connection = Substitute.For<DbConnection>();
            connection.State.Returns(ConnectionState.Open);
            var db = new Db(connection, DbConfig.Default);
            db.Connect();
            connection.DidNotReceive().Open();
        }
        [Fact]
        public async Task ConnectAsync_WhenCalled_OpensConnection()
        {
            var connection = Substitute.For<DbConnection>();
            var db = new Db(connection, DbConfig.Default);
            await db.ConnectAsync();
            await connection.Received(1).OpenAsync();
        }
        [Fact]
        public void Disconnect_WhenCalledOnOpenConnection_ClosesConnection()
        {
            var connection = Substitute.For<DbConnection>();
            connection.State.Returns(ConnectionState.Open);
            var db = new Db(connection, DbConfig.Default);
            db.Disconnect();
            connection.Received(1).Close();
        }
        [Fact]
        public void Disconnect_WhenCalledOnClosedConnection_DoesNothing()
        {
            var connection = Substitute.For<DbConnection>();
            connection.State.Returns(ConnectionState.Closed);
            var db = new Db(connection, DbConfig.Default);
            db.Disconnect();
            connection.DidNotReceive().Close();
        }
    }
    public class DbConstructorTests
    {
        [Fact]
        public void GivenDbWithExternalConnection_WhenDisposed_ConnectionIsNotDisposed()
        {
            var fakeConnection = Substitute.For<DbConnection>();

            var db = new Db(fakeConnection, DbConfig.Default);
            db.Dispose();

            // External connection should not be disposed with the Db
            fakeConnection.DidNotReceive().Dispose();
        }

        [Fact]
        public void GivenDbWithConnectionString_WhenDisposed_ConnectionIsDisposed()
        {
            var fakeConnection = Substitute.For<DbConnection>();
            fakeConnection.ConnectionString = "MyConnectionString";
            var fakeFactory = Substitute.For<DbProviderFactory>();

            fakeFactory.CreateConnection().Returns(fakeConnection);

            using (var db = new Db("", DbConfig.Default, fakeFactory))
            {
                // ensure the connection is actually instantiated
                // ReSharper disable UnusedVariable
                var connection = db.Connection;
                // ReSharper restore UnusedVariable
            }

            fakeConnection.Received().Dispose();
        }
    }
}
