using System.Data;
using System.Data.Common;

namespace Net.Code.ADONet
{
    class AdoNetProviderFactory : IConnectionFactory
    {
        private readonly string _providerInvariantName;

        public AdoNetProviderFactory(string providerInvariantName)
        {
            _providerInvariantName = providerInvariantName;
        }

        public IDbConnection CreateConnection(string connectionString)
        {
            var connection = DbProviderFactories.GetFactory(_providerInvariantName).CreateConnection();
            // ReSharper disable once PossibleNullReferenceException
            connection.ConnectionString = connectionString;
            return connection;
        }
    }
}