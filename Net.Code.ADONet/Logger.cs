using System.Diagnostics;

namespace Net.Code.ADONet;

/// <summary>
/// To enable logging, set the Log property of the Logger class
/// </summary>
public static class Logger
{
#if DEBUG
    public static Action<string> Log = s => Debug.WriteLine(s);
#else
    public static Action<string>? Log = null;
#endif

    internal static void LogCommand(IDbCommand command)
    {
        if (Log == null) return;
        Log.Invoke(command.CommandText);
        foreach (IDbDataParameter p in command.Parameters)
        {
            Log.Invoke($"{p.ParameterName} = {p.Value}");
        }
    }
}
