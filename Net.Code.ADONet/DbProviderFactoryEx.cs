using System.Data.Common;

namespace Net.Code.ADONet
{
    static class DbProviderFactoryEx 
    {
        public static DbConnection CreateConnection(this DbProviderFactory factory, string connectionString)
        {
            var connection = factory.CreateConnection();
            connection.ConnectionString = connectionString;
            return connection;
        }
    }
}