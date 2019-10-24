#if NETFRAMEWORK
using System.Configuration;

namespace Net.Code.ADONet
{
    public class DbFactory
    {
        /// <summary>
        /// Factory method, instantiating the Db class from the first connectionstring 
        /// in the app.config or web.config file.
        /// </summary>
        /// <returns>Db</returns>
        public static Db FromConfig() => FromConfig(ConfigurationManager.ConnectionStrings[0]);

        /// <summary>
        /// Factory method, instantiating the Db class from a named connectionstring 
        /// in the app.config or web.config file.
        /// </summary>
        public static Db FromConfig(string connectionStringName)
            => FromConfig(ConfigurationManager.ConnectionStrings[connectionStringName]);

        private static Db FromConfig(ConnectionStringSettings connectionStringSettings)
        {
            var connectionString = connectionStringSettings.ConnectionString;
            var providerName = connectionStringSettings.ProviderName;
            return new Db(connectionString, providerName);
        }
    }
}

#endif