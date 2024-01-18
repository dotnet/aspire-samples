
namespace ApiService.Tests;

public sealed class DistributedApplicationFixture<TEntryPoint> : IAsyncLifetime where TEntryPoint : class
{
    private readonly DistributedApplicationFactory<TEntryPoint> _factory;

    public DistributedApplicationFixture()
    {
        _factory = new DistributedApplicationFactory<TEntryPoint>();
    }

    public async Task InitializeAsync()
    {
        await _factory.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    public HttpClient CreateClient(string resourceName, string? endpointName = default)
    {
        return _factory.CreateClient(resourceName, endpointName);
    }
}
