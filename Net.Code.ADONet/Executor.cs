namespace Net.Code.ADONet;

public partial class CommandBuilder
{
    private class Executor
    {
        private readonly DbCommand _command;
        public Executor(DbCommand command)
        {
            _command = command;
        }

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
            Logger.LogCommand(_command);
            if (_command.Connection.State == ConnectionState.Closed)
                _command.Connection.Open();
            return _command;
        }
    }
}
