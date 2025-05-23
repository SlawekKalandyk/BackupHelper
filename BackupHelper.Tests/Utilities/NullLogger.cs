using Microsoft.Extensions.Logging;

namespace BackupHelper.Tests.Utilities;

public class NullLogger<T> : ILogger<T>
{
    IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;
    bool ILogger.IsEnabled(LogLevel logLevel) => false;
    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();
        public void Dispose() { }
    }
}