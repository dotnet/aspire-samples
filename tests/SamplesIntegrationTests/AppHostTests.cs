// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task TestEndpointsReturnOk(TestEndpoints testEndpoints)
    {
        var appHostName = testEndpoints.AppHost!;
        var resourceEndpoints = testEndpoints.ResourceEndpoints!;

        var appHostPath = $"{appHostName}.dll";
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, testOutput);
        var projects = appHost.Resources.OfType<ProjectResource>();
        await using var app = await appHost.BuildAsync();

        var appHostLogs = app.GetAppHostLogs();
        var resourceLogs = app.GetResourceLogs();

        await app.StartAsync(waitForResourcesToStart: true);

        // Workaround race in DCP that can result in resources being deleted while they are still starting
        await Task.Delay(100);

        foreach (var resource in resourceEndpoints.Keys)
        {
            var endpoints = resourceEndpoints[resource];

            if (endpoints.Count == 0)
            {
                // No test endpoints so ignore this resource
                continue;
            }

            HttpResponseMessage? response = null;

            using var client = app.CreateHttpClient(resource, null, clientBuilder =>
            {
                clientBuilder
                    .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan)
                    .AddStandardResilienceHandler(resilience =>
                    {
                        resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);
                        resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                        resilience.Retry.MaxRetryAttempts = 5;
                        resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(300);
                    });
            });

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
                    Assert.True(HttpStatusCode.OK == response.StatusCode, $"Endpoint '{client.BaseAddress}{path.TrimStart('/')}' for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}' returned status code {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Error when calling endpoint '{client.BaseAddress}{path.TrimStart('/')} for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}': {ex.Message}");
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
            [new TestEndpoints("AspireShop.AppHost", new() {
                { "catalogdbmanager", ["/alive", "/health"] },
                { "catalogservice", ["/alive", "/health"] },
                // Can't send non-gRPC requests over non-TLS connection to the BasketService unless client is manually configured to use HTTP/2
                //{ "basketservice", ["/alive", "/health"] },
                { "frontend", ["/alive", "/health", "/"] }
            })],
            [new TestEndpoints("AspireJavaScript.AppHost", new() {
                { "weatherapi", ["/alive", "/health", "/weatherforecast"] },
                { "angular", ["/"] },
                { "react", ["/"] },
                { "vue", ["/"] }
            })],
            [new TestEndpoints("AspireWithNode.AppHost", new() {
                { "weatherapi", ["/alive", "/health", "/weatherforecast"] },
                { "frontend", ["/alive", "/health", "/"] }
            })],
            [new TestEndpoints("ClientAppsIntegration.AppHost", new() {
                { "apiservice", ["/alive", "/health", "/weatherforecast"] }
            })],
            [new TestEndpoints("DatabaseContainers.AppHost", new() {
                { "apiservice", ["/alive", "/health", "/todos", "/todos/1", "/catalog", "/catalog/1", "/addressbook", "/addressbook/1"] }
            })],
            [new TestEndpoints("DatabaseMigrations.AppHost", new() {
                { "api", ["/alive", "/health", "/"] }
            })],
            [new TestEndpoints("MetricsApp.AppHost", new() {
                { "app", ["/alive", "/health"] },
                { "grafana", ["/"] }
            })],
            [new TestEndpoints("OrleansVoting.AppHost", new() {
                { "voting-fe", ["/alive", "/health", "/", "/api/votes"] }
            })],
            [new TestEndpoints("VolumeMount.AppHost", new() {
                { "blazorweb", ["/alive", "/ApplyDatabaseMigrations", "/health", "/"] }
            })]
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

public class TestEndpoints : IXunitSerializable
{
    // Required for deserialization
    public TestEndpoints() { }

    public TestEndpoints(string appHost, Dictionary<string, List<string>> resourceEndpoints)
    {
        AppHost = appHost;
        ResourceEndpoints = resourceEndpoints;
    }

    public string? AppHost { get; set; }

    public Dictionary<string, List<string>>? ResourceEndpoints { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        AppHost = info.GetValue<string>(nameof(AppHost));
        ResourceEndpoints = JsonSerializer.Deserialize< Dictionary<string, List<string>>>(info.GetValue<string>(nameof(ResourceEndpoints)));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(AppHost), AppHost);
        info.AddValue(nameof(ResourceEndpoints), JsonSerializer.Serialize(ResourceEndpoints));
    }

    public override string? ToString() => $"{AppHost} ({ResourceEndpoints?.Count ?? 0} resources)";
}
