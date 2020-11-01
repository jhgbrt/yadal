using System;
using System.Data;
using System.Diagnostics;

namespace Net.Code.ADONet
{
    /// <summary>
    /// To enable logging, set the Log property of the Logger class
    /// </summary>
    public class Logger
    {
#if DEBUG
        public static Action<string> Log = s => { Debug.WriteLine(s); };
#else
        public static Action<string> Log = s => { };
#endif

        internal static void LogCommand(IDbCommand command)
        {
            Log(command.CommandText);
            foreach (IDbDataParameter p in command.Parameters)
            {
                Log($"{p.ParameterName} = {p.Value}");
            }
        }
    }
}