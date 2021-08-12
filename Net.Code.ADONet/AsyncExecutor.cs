namespace Net.Code.ADONet;

public partial class CommandBuilder
{
    private class AsyncExecutor
    {
        private readonly DbCommand _command;

        public AsyncExecutor(DbCommand command)
        {
            _command = command;
        }

        /// <summary>
        /// executes the query as a datareader
        /// </summary>
        public async Task<DbDataReader> Reader()
        {
            var command = await PrepareAsync().ConfigureAwait(false);
            return await command.ExecuteReaderAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the command, returning the first column of the first result as a scalar value
        /// </summary>
        public async Task<object> Scalar()
        {
            var command = await PrepareAsync().ConfigureAwait(false);
            return await command.ExecuteScalarAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public async Task<int> NonQuery()
        {
            var command = await PrepareAsync().ConfigureAwait(false);
            return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task<DbCommand> PrepareAsync()
        {
            Logger.LogCommand(_command);
            if (_command.Connection.State == ConnectionState.Closed)
                await _command.Connection.OpenAsync().ConfigureAwait(false);
            return _command;
        }
    }
}
