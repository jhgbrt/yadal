using System.Data;
using System.Data.Common;
using NSubstitute;
using Xunit;


namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    public class DbTests
    {
        [Fact]
        public void Connect_WhenCalled_OpensConnection()
        {
            var connection = Substitute.For<DbConnection>();
            var db = new Db(connection, DbConfig.Default);
            db.Connect();
            connection.Received(1).Open();
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
