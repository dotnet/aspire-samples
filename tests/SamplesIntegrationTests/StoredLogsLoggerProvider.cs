using Microsoft.Extensions.Logging;

namespace SamplesIntegrationTests;

internal class StoredLogsLoggerProvider(LoggerLogStore logStore) : ILoggerProvider
{
    private readonly LoggerExternalScopeProvider _scopeProvider = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new StoredLogsLogger(logStore, _scopeProvider, categoryName);
    }

    public void Dispose()
    {
    }
}
