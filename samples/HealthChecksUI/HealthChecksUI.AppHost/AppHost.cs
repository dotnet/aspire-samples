var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.HealthChecksUI_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithFriendlyUrls(displayText: "API");

var webFrontend = builder.AddProject<Projects.HealthChecksUI_Web>("webfrontend")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithHttpHealthCheck("/health")
    .WithFriendlyUrls("Web Frontend")
    .WithExternalHttpEndpoints();

var healthChecksUI = builder.AddHealthChecksUI("healthchecksui")
    .WithReference(apiService)
    .WithReference(webFrontend)
    .WithFriendlyUrls("HealthChecksUI Dashboard", "http")
    // This will make the HealthChecksUI dashboard available from external networks when deployed.
    // In a production environment, you should consider adding authentication to the ingress layer
    // to restrict access to the dashboard.
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode)
{
    healthChecksUI.WithHostPort(7230);
}

builder.Build().Run();


static class UrlHelpers
{
    extension<T>(IResourceBuilder<T> builder) where T : IResource
    {
        public IResourceBuilder<T> WithFriendlyUrls(string? displayText = null, string? endpointName = null, string? path = null)
        {
            return builder.WithUrls(c =>
            {
                List<string?> endpointNames = [endpointName, "https", "http"];
                var endpoint = endpointNames
                    .Where(n => n is not null)
                    .Select(n => c.GetEndpoint(n!))
                    .FirstOrDefault(e => e?.Exists ?? false);

                if (endpoint is null) return;

                displayText ??= builder.Resource.Name;
                foreach (var url in c.Urls)
                {
                    url.DisplayLocation = UrlDisplayLocation.DetailsOnly;
                }

                c.Urls.Add(new()
                {
                    Endpoint = endpoint,
                    DisplayText = displayText,
                    DisplayLocation = UrlDisplayLocation.SummaryAndDetails,
                    Url = path ?? "/"
                });
                
            });
        }
    }
}