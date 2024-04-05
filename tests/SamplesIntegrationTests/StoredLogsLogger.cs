using Microsoft.Extensions.Logging;

namespace SamplesIntegrationTests;

internal class StoredLogsLogger(LoggerLogStore logStore, LoggerExternalScopeProvider scopeProvider, string categoryName) : ILogger
{
    public string CategoryName { get; } = categoryName;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopeProvider.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        logStore.AddLog(CategoryName, logLevel, formatter(state, exception), exception);
    }
}
