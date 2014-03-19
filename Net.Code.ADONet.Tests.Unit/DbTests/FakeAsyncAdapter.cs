using System.Data;
using System.Threading.Tasks;

namespace Net.Code.ADONet.Tests.Unit.DbTests
{
    public class FakeAsyncAdapter : IAsyncAdapter
    {
        public Task<int> ExecuteNonQueryAsync(IDbCommand command)
        {
            return Task.Run(() => 1);
        }

        public Task<object> ExecuteScalarAsync(IDbCommand command)
        {
            return Task.Run(() => (object)1);
        }

        public Task<IDataReader> ExecuteReaderAsync(IDbCommand command)
        {
            return Task.Run(() => (IDataReader)null);
        }

        public Task OpenConnectionAsync(IDbConnection connection)
        {
            return Task.Run(() => {});
        }
    }
}