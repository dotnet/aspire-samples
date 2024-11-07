// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SamplesIntegrationTests.Infrastructure;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SamplesIntegrationTests;

public class AppHostTests(ITestOutputHelper testOutput)
{
    [Theory]
    [MemberData(nameof(AppHostAssemblies))]
    public async Task AppHostRunsCleanly(string appHostPath)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, testOutput);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(120));
        await app.WaitForResourcesAsync().WaitAsync(TimeSpan.FromSeconds(120));

        app.EnsureNoErrorsLogged();

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

        await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(120));
        await app.WaitForResourcesAsync().WaitAsync(TimeSpan.FromSeconds(120));

        if (testEndpoints.WaitForResources?.Count > 0)
        {
            // Wait until each resource transitions to the required state
            var timeout = TimeSpan.FromMinutes(5);
            foreach (var (ResourceName, TargetState) in testEndpoints.WaitForResources)
            {
                await app.WaitForResource(ResourceName, TargetState).WaitAsync(timeout);
            }
        }

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
                        resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                        resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                        resilience.Retry.MaxRetryAttempts = 30;
                        resilience.CircuitBreaker.SamplingDuration = resilience.AttemptTimeout.Timeout * 2;
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

                testOutput.WriteLine($"Calling endpoint '{client.BaseAddress}{path.TrimStart('/')} for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}'");
                try
                {
                    response = await client.GetAsync(path);
                }
                catch(Exception e)
                {
                    throw new XunitException($"Failed calling endpoint '{client.BaseAddress}{path.TrimStart('/')} for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}'", e);
                }

                Assert.True(HttpStatusCode.OK == response.StatusCode, $"Endpoint '{client.BaseAddress}{path.TrimStart('/')}' for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}' returned status code {response.StatusCode}");
            }
        }

        app.EnsureNoErrorsLogged();

        await app.StopAsync();
    }

    public static TheoryData<string> AppHostAssemblies()
    {
        var appHostAssemblies = GetSamplesAppHostAssemblyPaths();
        var theoryData = new TheoryData<string, bool>();
        return new(appHostAssemblies.Select(p => Path.GetRelativePath(AppContext.BaseDirectory, p)));
    }

    public static TheoryData<TestEndpoints> TestEndpoints() =>
        new([
            #if NET8_0
            new TestEndpoints("AspireShop.AppHost", new() {
                { "catalogdbmanager", ["/alive", "/health"] },
                { "catalogservice", ["/alive", "/health"] },
                // Can't send non-gRPC requests over non-TLS connection to the BasketService unless client is manually configured to use HTTP/2
                //{ "basketservice", ["/alive", "/health"] },
                { "frontend", ["/alive", "/health", "/"] }
            }),
            new TestEndpoints("AspireWithDapr.AppHost", new() {
                { "apiservice", ["/alive", "/health", "/weatherforecast"] },
                { "webfrontend", ["/alive", "/health", "/", "/weather"] }
            }),
            new TestEndpoints("AspireJavaScript.AppHost", new() {
                { "weatherapi", ["/alive", "/health", "/weatherforecast"] },
                { "angular", ["/"] },
                { "react", ["/"] },
                { "vue", ["/"] }
            }),
            new TestEndpoints("AspireWithNode.AppHost", new() {
                { "weatherapi", ["/alive", "/health", "/weatherforecast"] },
                { "frontend", ["/alive", "/health", "/"] }
            }),
            new TestEndpoints("AspireWithPython.AppHost", new() {
                { "instrumented-python-app", ["/"] }
            }),
            new TestEndpoints("ClientAppsIntegration.AppHost", new() {
                { "apiservice", ["/alive", "/health", "/weatherforecast"] }
            }),
            new TestEndpoints("ContainerBuild.AppHost", new() {
                { "ginapp", ["/"] }
            }),
            new TestEndpoints("DatabaseContainers.AppHost", new() {
                { "apiservice", ["/alive", "/health", "/todos", "/todos/1", "/catalog", "/catalog/1", "/addressbook", "/addressbook/1"] }
            }),
            new TestEndpoints("DatabaseMigrations.AppHost", new() {
                { "api", ["/alive", "/health", "/"] }
            })
            {
                WaitForResources = [new("migration", KnownResourceStates.Finished)]
            },
            new TestEndpoints("HealthChecksUI.AppHost", new() {
                { "apiservice", ["/alive", "/health", "/weatherforecast"] },
                { "webfrontend", ["/alive", "/health", "/", "/weather"] },
                { "healthchecksui", ["/"] }
            }),
            new TestEndpoints("MetricsApp.AppHost", new() {
                { "app", ["/alive", "/health"] },
                { "grafana", ["/"] }
            }),
            new TestEndpoints("OrleansVoting.AppHost", new() {
                { "voting-fe", ["/alive", "/health", "/", "/api/votes"] }
            }),
            new TestEndpoints("VolumeMount.AppHost", new() {
                { "blazorweb", ["/alive", "/ApplyDatabaseMigrations", "/health", "/"] }
            }),
            #elif NET9_0
            new TestEndpoints("ImageGallery.AppHost", new() {
                { "frontend", ["/alive", "/health", "/"] }
            }),
            #endif
        ]);

    private static IEnumerable<string> GetSamplesAppHostAssemblyPaths()
    {
        // All the AppHost projects are referenced by this project so we can find them by looking for all their assemblies in the base directory
        return Directory.GetFiles(AppContext.BaseDirectory, "*.AppHost.dll")
            .Where(fileName => !fileName.EndsWith("Aspire.Hosting.AppHost.dll", StringComparison.OrdinalIgnoreCase));
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

    public List<ResourceWait>? WaitForResources { get; set; }

    public Dictionary<string, List<string>>? ResourceEndpoints { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        AppHost = info.GetValue<string>(nameof(AppHost));
        WaitForResources = JsonSerializer.Deserialize<List<ResourceWait>>(info.GetValue<string>(nameof(WaitForResources)));
        ResourceEndpoints = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(info.GetValue<string>(nameof(ResourceEndpoints)));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(AppHost), AppHost);
        info.AddValue(nameof(WaitForResources), JsonSerializer.Serialize(WaitForResources));
        info.AddValue(nameof(ResourceEndpoints), JsonSerializer.Serialize(ResourceEndpoints));
    }

    public override string? ToString() => $"{AppHost} ({ResourceEndpoints?.Count ?? 0} resources)";

    public class ResourceWait(string resourceName, string targetState)
    {
        public string ResourceName { get; } = resourceName;

        public string TargetState { get; } = targetState;

        public void Deconstruct(out string resourceName, out string targetState)
        {
            resourceName = ResourceName;
            targetState = TargetState;
        }
    }
}
