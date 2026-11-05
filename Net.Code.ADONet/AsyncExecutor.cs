using System.Threading;

using Microsoft.Extensions.Logging;

namespace Net.Code.ADONet;

public partial class CommandBuilder
{
    private record struct AsyncExecutor(DbCommand Command, ILogger logger)
    {
        /// <summary>
        /// executes the query as a datareader
        /// </summary>
        public async Task<DbDataReader> Reader(CancellationToken cancellationToken)
        {
            var command = await PrepareAsync(cancellationToken).ConfigureAwait(false);
            return await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the command, returning the first column of the first result as a scalar value
        /// </summary>
        public async Task<object?> Scalar(CancellationToken cancellationToken)
        {
            var command = await PrepareAsync(cancellationToken).ConfigureAwait(false);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public async Task<int> NonQuery(CancellationToken cancellationToken)
        {
            var command = await PrepareAsync(cancellationToken).ConfigureAwait(false);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<DbCommand> PrepareAsync(CancellationToken cancellationToken)
        {
            Logger.LogCommand(logger, Command);
            var connection = Command.Connection ?? throw new InvalidOperationException("The command has no connection");
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return Command;
        }
    }
}
