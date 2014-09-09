using System.Data.SqlClient;
using NUnit.Framework;

namespace Net.Code.ADONet.Tests.Unit
{
    [TestFixture]
    public class AdoNetProviderFactoryTests
    {
        [Test]
        public void CreateConnection_SqlClientProviderFactory_CreatesSqlConnection()
        {
            var f = new AdoNetProviderFactory("System.Data.SqlClient");
            var connection = f.CreateConnection("");
            Assert.IsInstanceOf(typeof(SqlConnection), connection);
        }
    }
}
