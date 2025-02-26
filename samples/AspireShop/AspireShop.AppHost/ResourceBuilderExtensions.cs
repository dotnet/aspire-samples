using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

internal static class ResourceBuilderExtensions
{
    /// <summary>
    /// Adds a command to the resource that sends an HTTP request to the specified path.
    /// </summary>
    public static IResourceBuilder<TResource> WithHttpCommand<TResource>(this IResourceBuilder<TResource> builder,
        string path,
        string displayName,
        string? endpointName = default,
        HttpMethod? method = default,
        string? iconName = default)
        where TResource : IResourceWithEndpoints
        => WithHttpCommandImpl(builder, path, displayName, endpointName is not null ? [endpointName] : ["https", "http"], method, iconName);

    /// <summary>
    /// Adds a command to the resource that sends an HTTP request to the specified path.
    /// </summary>
    public static IResourceBuilder<TResource> WithHttpCommand<TResource>(this IResourceBuilder<TResource> builder,
        string path,
        string displayName,
        string[] endpointNames,
        HttpMethod? method = default,
        string? iconName = default)
        where TResource : IResourceWithEndpoints
        => WithHttpCommandImpl(builder, path, displayName, endpointNames, method, iconName);

    private static IResourceBuilder<TResource> WithHttpCommandImpl<TResource>(this IResourceBuilder<TResource> builder,
        string path,
        string displayName,
        string[] endpointNames,
        HttpMethod? method,
        string? iconName)
        where TResource : IResourceWithEndpoints
    {
        method ??= HttpMethod.Post;

        var endpoints = builder.Resource.GetEndpoints();
        var endpoint = endpoints.FirstOrDefault(e => endpointNames.Contains(e.EndpointName, StringComparer.OrdinalIgnoreCase))
            ?? throw new DistributedApplicationException($"Could not create HTTP command for resource '{builder.Resource.Name}' as no endpoint with one of the following names was found: '{string.Join(", ", endpointNames)}'");

        var commandName = $"http-{method.Method.ToLowerInvariant()}-request";

        builder.WithCommand(commandName, displayName, async context =>
        {
            if (!endpoint.IsAllocated)
            {
                return new ExecuteCommandResult { Success = false, ErrorMessage = "Endpoints are not yet allocated." };
            }

            var uri = new UriBuilder(endpoint.Url) { Path = path }.Uri;
            var httpClient = context.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            var request = new HttpRequestMessage(method, uri);
            try
            {
                var response = await httpClient.SendAsync(request, context.CancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                return new ExecuteCommandResult { Success = false, ErrorMessage = ex.Message };
            }
            return new ExecuteCommandResult { Success = true };
        },
        iconName: iconName,
        iconVariant: IconVariant.Regular);

        return builder;
    }
}
