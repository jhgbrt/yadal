using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Net.Code.ADONet.Tests.Unit
{
    [TestClass]
    public class AdoNetProviderFactoryTests
    {
        [TestMethod]
        public void CreateConnection_SqlClientProviderFactory_CreatesSqlConnection()
        {
            var f = new AdoNetProviderFactory("System.Data.SqlClient");
            var connection = f.CreateConnection("");
            Assert.IsInstanceOfType(connection, typeof(SqlConnection));
        }
    }
}
