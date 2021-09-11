namespace Net.Code.ADONet;

public partial class CommandBuilder
{
    private record struct Executor(DbCommand Command)
    {
        /// <summary>
        /// executes the query as a datareader
        /// </summary>
        public DbDataReader Reader() => Prepare().ExecuteReader();

        /// <summary>
        /// Executes the command, returning the first column of the first result as a scalar value
        /// </summary>
        public object Scalar() => Prepare().ExecuteScalar();

        /// <summary>
        /// Executes the command as a SQL statement, returning the number of rows affected
        /// </summary>
        public int NonQuery() => Prepare().ExecuteNonQuery();

        private DbCommand Prepare()
        {
            Logger.LogCommand(Command);
            if (Command.Connection.State == ConnectionState.Closed)
                Command.Connection.Open();
            return Command;
        }
    }
}
