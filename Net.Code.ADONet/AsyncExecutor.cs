using Microsoft.Extensions.Logging;

namespace Net.Code.ADONet;

public partial class CommandBuilder
{
    private record struct AsyncExecutor(DbCommand Command, ILogger logger)
    {
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
            Logger.LogQuery(logger, LogLevel.Trace, Command.CommandText);
            if (Command.Connection.State == ConnectionState.Closed)
                await Command.Connection.OpenAsync().ConfigureAwait(false);
            return Command;
        }

    }
}
