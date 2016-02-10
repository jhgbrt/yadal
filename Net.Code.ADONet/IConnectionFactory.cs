using System.Data;

namespace Net.Code.ADONet
{
    public interface IConnectionFactory
    {
        /// <summary>
        /// Create the ADO.Net IDbConnection
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>the connection</returns>
        IDbConnection CreateConnection(string connectionString);
    }
}