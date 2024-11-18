using Xunit.Abstractions;
using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Xunit.Sdk;

internal class XUnitLogger : ILogger
{
    interface IWriter
    {
        public void WriteLine(string message);
    }
    class TestOutputHelperWriter(ITestOutputHelper output):IWriter
    {
        public void WriteLine(string message) => output.WriteLine(message);
    }
    class MessageSinkWriter(IMessageSink sink) : IWriter
    {
        public void WriteLine(string message) => sink.OnMessage(new DiagnosticMessage(message));
    }

    private readonly IWriter _testOutputHelper;
    private readonly LoggerExternalScopeProvider _scopeProvider;

    public static ILogger CreateLogger(ITestOutputHelper testOutputHelper) => new XUnitLogger(new TestOutputHelperWriter(testOutputHelper), new LoggerExternalScopeProvider());
    public static ILogger CreateLogger(IMessageSink messageSink) => new XUnitLogger(new MessageSinkWriter(messageSink), new LoggerExternalScopeProvider());

    private XUnitLogger(IWriter testOutputHelper, LoggerExternalScopeProvider scopeProvider)
    {
        _testOutputHelper = testOutputHelper;
        _scopeProvider = scopeProvider;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable BeginScope<TState>(TState state) where TState: notnull => _scopeProvider.Push(state);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var sb = new StringBuilder();
        sb.Append(GetLogLevelString(logLevel))
          .Append(formatter(state, exception));

        if (exception != null)
        {
            sb.Append('\n').Append(exception);
        }

        // Append scopes
        _scopeProvider.ForEachScope((scope, state) =>
        {
            state.Append("\n => ");
            state.Append(scope);
        }, sb);

        _testOutputHelper.WriteLine(sb.ToString());
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel.ToString();
}