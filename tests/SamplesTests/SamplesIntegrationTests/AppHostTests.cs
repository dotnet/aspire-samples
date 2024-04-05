using System.Net;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

public class AppHostTests(ITestOutputHelper testOutput)
{
    [Theory]
    [MemberData(nameof(AppHostAssemblies))]
    public async Task AppHostRunsCleanly(string appHostPath)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, testOutput);
        await using var app = await appHost.BuildAsync();

        var appHostLogs = app.GetAppHostLogs();
        var resourceLogs = app.GetResourceLogs();

        await app.StartAsync(waitForResourcesToStart: true);

        // Workaround race in DCP that can result in resources being deleted while they are still starting
        await Task.Delay(100);

        appHostLogs.EnsureNoErrors();
        resourceLogs.EnsureNoErrors(resource => resource is (ProjectResource or ExecutableResource) and not NodeAppResource);

        await app.StopAsync();
    }

    [Theory]
    [MemberData(nameof(TestEndpoints))]
    public async Task TestEndpointsReturnOk(string appHostName, Dictionary<string, string[]> testEndpoints)
    {
        var appHostPath = $"{appHostName}.dll";
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, testOutput);
        var projects = appHost.Resources.OfType<ProjectResource>();
        await using var app = await appHost.BuildAsync();

        var appHostLogs = app.GetAppHostLogs();
        var resourceLogs = app.GetResourceLogs();

        await app.StartAsync(waitForResourcesToStart: true);

        // Workaround race in DCP that can result in resources being deleted while they are still starting
        await Task.Delay(100);

        foreach (var resource in testEndpoints.Keys)
        {
            var endpoints = testEndpoints[resource];

            if (endpoints.Length == 0)
            {
                // No test endpoints so ignore this resource
                continue;
            }

            HttpResponseMessage? response = null;

            using var client = app.CreateHttpClient(resource);

            foreach (var path in endpoints)
            {
                if (string.Equals("/ApplyDatabaseMigrations", path, StringComparison.OrdinalIgnoreCase)
                    && projects.FirstOrDefault(p => string.Equals(p.Name, resource, StringComparison.OrdinalIgnoreCase)) is { } project)
                {
                    await app.TryApplyEfMigrationsAsync(project);
                    continue;
                }

                try
                {
                    response = await client.GetAsync(path);
                    Assert.True(HttpStatusCode.OK == response.StatusCode, $"Endpoint '{path}' for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}' returned status code {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Error when calling endpoint '{path} for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}': {ex.Message}");
                }
            }
        }

        appHostLogs.EnsureNoErrors();
        resourceLogs.EnsureNoErrors(resource => resource is (ProjectResource or ExecutableResource) and not NodeAppResource);

        await app.StopAsync();
    }

    public static object[][] AppHostAssemblies()
    {
        var appHostAssemblies = GetSamplesAppHostAssemblyPaths();
        return appHostAssemblies.Select(p => new object[] { Path.GetRelativePath(AppContext.BaseDirectory, p) }).ToArray();
    }

    public static object[][] TestEndpoints() =>
        [
            ["AspireShop.AppHost", new Dictionary<string, string[]> {
                { "catalogdbmanager", ["/alive", "/health"] },
                { "catalogservice", ["/alive", "/health"] },
                // Can't send non-gRPC requests over non-TLS connection to the BasketService unless client is manually configured to use HTTP/2
                //{ "basketservice", ["/alive", "/health"] },
                { "frontend", ["/alive", "/health", "/"] }
            }],
            ["AspireJavaScript.AppHost", new Dictionary<string, string[]> {
                { "weatherapi", ["/alive", "/health", "/weatherforecast"] },
                { "angular", ["/"] },
                { "react", ["/"] },
                { "vue", ["/"] }
            }],
            ["AspireWithNode.AppHost", new Dictionary<string, string[]> {
                { "weatherapi", ["/alive", "/health", "/weatherforecast"] },
                { "frontend", ["/alive", "/health", "/"] }
            }],
            ["ClientAppsIntegration.AppHost", new Dictionary<string, string[]> {
                { "apiservice", ["/alive", "/health", "/weatherforecast"] }
            }],
            ["DatabaseContainers.AppHost", new Dictionary<string, string[]> {
                { "apiservice", ["/alive", "/health", "/todos", "/todos/1", "/catalog", "/catalog/1", "/addressbook", "/addressbook/1"] }
            }],
            ["DatabaseMigrations.AppHost", new Dictionary<string, string[]> {
                { "api", ["/alive", "/health", "/"] }
            }],
            ["MetricsApp.AppHost", new Dictionary<string, string[]> {
                { "app", ["/alive", "/health"] },
                { "grafana", ["/"] }
            }],
            ["OrleansVoting.AppHost", new Dictionary<string, string[]> {
                { "voting-fe", ["/alive", "/health", "/", "/api/votes"] }
            }],
            ["VolumeMount.AppHost", new Dictionary<string, string[]> {
                { "blazorweb", ["/alive", "/ApplyDatabaseMigrations", "/health", "/"] }
            }]
        ];

    private static IEnumerable<string> GetSamplesAppHostAssemblyPaths()
    {
        // All the AppHost projects are referenced by this project so we can find them by looking for all their assemblies in the base directory
        return Directory.GetFiles(AppContext.BaseDirectory, "*.AppHost.dll")
            .Where(fileName => !fileName.EndsWith("Aspire.Hosting.AppHost.dll", StringComparison.OrdinalIgnoreCase)
                               // Known issue in preview.5 with Dapr and randomization of resource names that occurs in integration testing
                               && !fileName.EndsWith("AspireWithDapr.AppHost.dll", StringComparison.OrdinalIgnoreCase));
    }
}
