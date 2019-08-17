using System.Configuration;
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
            var connection = Substitute.For<IDbConnection>();
            var db = new Db(connection, DbConfig.Default);
            db.Connect();
            connection.Received(1).Open();
        }
        [Fact]
        public void ProviderName_WhenCalled_ReturnsProviderName()
        {
            var db = new Db(Substitute.For<IDbConnection>(), DbConfig.FromProviderName("TEST"));
            Assert.Equal("TEST", db.ProviderName);
        }
    }
    public class DbConstructorTests
    {
        [Fact]
        public void GivenDbWithExternalConnection_WhenDisposed_ConnectionIsNotDisposed()
        {
            var fakeConnection = Substitute.For<IDbConnection>();

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


#if NETFRAMEWORK
        [Fact]
        public void FromConfig_ReturnsDbWithFirstConfigurationSetting()
        {
            var db = Db.FromConfig();
            Assert.Equal(ConfigurationManager.ConnectionStrings["firstConnectionString"].ConnectionString, db.ConnectionString);
        }
        [Fact]
        public void FromConfig_ReturnsDbWithNamedConfigurationSetting()
        {
            var db = Db.FromConfig("secondConnectionString");
            Assert.Equal(ConfigurationManager.ConnectionStrings["secondConnectionString"].ConnectionString, db.ConnectionString);
        }
#endif
    }
}
