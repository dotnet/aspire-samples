namespace MetricsApp.AppHost.ContainerProxy;

internal sealed class ContainerProxyAnnotation : IResourceAnnotation
{
    public ContainerProxyAnnotation(IResourceWithEndpoints proxiedResource)
    {
        ProxiedResource = proxiedResource;
    }

    public string? FilePath { get; set; }

    public IResourceWithEndpoints ProxiedResource { get; }
}
