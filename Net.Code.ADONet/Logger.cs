using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Diagnostics;

namespace Net.Code.ADONet;

/// <summary>
/// To enable logging, set the Log property of the Logger class
/// </summary>
public static partial class Logger
{

    [LoggerMessage(EventId = 1, Message = "SQL Query : {query}")]
    public static partial void LogQuery(ILogger logger, LogLevel level, string query);
    [LoggerMessage(EventId = 2, Message = "Parameters: {Parameters}")]
    public static partial void LogParameters(ILogger logger, LogLevel level, string parameters);

    [Obsolete("This is no longer used, pas an ILogger to the Db constructor instead", true)]
    public static Action<string>? Log = null;

    internal static void LogCommand(ILogger logger, IDbCommand command)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            LogQuery(logger, LogLevel.Trace, command.CommandText);

            var kv = from p in command.Parameters.OfType<DbParameter>()
                     select $"{p.ParameterName}: \"{p.Value}\"";
            LogParameters(logger, LogLevel.Trace, $"{{ {string.Join(",", kv)} }}");
        }

    }
}
