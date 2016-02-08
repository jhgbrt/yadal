using System.Configuration;
using System.Data;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    [TestClass]
    public class DbTests
    {
        [TestMethod]
        public void Connect_WhenCalled_OpensConnection()
        {
            var connection = Substitute.For<IDbConnection>();
            var db = new Db(connection);
            db.Connect();
            connection.Received(1).Open();
        }
    }

    [TestClass]
    public class DbConstructorTests
    {

        [TestMethod]
        public void GivenDbWithExternalConnection_WhenDisposed_ConnectionIsNotDisposed()
        {
            var fakeConnection = Substitute.For<IDbConnection>();

            var db = new Db(fakeConnection);
            db.Dispose();

            // External connection should not be disposed with the Db
            fakeConnection.DidNotReceive().Dispose();
        }

        [TestMethod]
        public void GivenDbWithConnectionString_WhenDisposed_ConnectionIsDisposed()
        {
            var fakeConnection = Substitute.For<IDbConnection>();

            var fakeFactory = Substitute.For<IConnectionFactory>();

            fakeFactory.CreateConnection("").Returns(fakeConnection);

            using (var db = new Db("", fakeFactory))
            {
                // ensure the connection is actually instantiated
                // ReSharper disable UnusedVariable
                var connection = db.Connection;
                // ReSharper restore UnusedVariable
            }

            fakeConnection.Received().Dispose();
        }

        [TestMethod]
        public void FromConfig_ReturnsDbWithFirstConfigurationSetting()
        {
            var db = Db.FromConfig();
            Assert.AreEqual(ConfigurationManager.ConnectionStrings["firstConnectionString"].ConnectionString, db.ConnectionString);
        }
        [TestMethod]
        public void FromConfig_ReturnsDbWithNamedConfigurationSetting()
        {
            var db = Db.FromConfig("secondConnectionString");
            Assert.AreEqual(ConfigurationManager.ConnectionStrings["secondConnectionString"].ConnectionString, db.ConnectionString);
        }
    }
}
