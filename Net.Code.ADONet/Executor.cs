using Microsoft.Extensions.Logging;

namespace Net.Code.ADONet;

public partial class CommandBuilder
{
    private record struct Executor(DbCommand Command, ILogger logger)
    {
        /// <summary>
        /// executes the query as a datareader
        /// </summary>
        public DbDataReader Reader() => Prepare().ExecuteReader();

        /// <summary>
        /// Executes the command, returning the first column of the first result as a scalar value
        /// </summary>
        public object? Scalar() => Prepare().ExecuteScalar();

        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public int NonQuery() => Prepare().ExecuteNonQuery();

        private DbCommand Prepare()
        {
            Logger.LogCommand(logger, Command);
            var connection = Command.Connection ?? throw new InvalidOperationException("The command has no connection");
            if (connection.State == ConnectionState.Closed)
                connection.Open();
            return Command;
        }
    }
}
