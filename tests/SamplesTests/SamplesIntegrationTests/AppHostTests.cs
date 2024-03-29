using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

//[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]

namespace SamplesIntegrationTests;

public class AppHostTests(ITestOutputHelper testOutput)
{
    [Theory]
    [MemberData(nameof(AppHostProjects))]
    public async Task AppHostRunsCleanly(string projectName, string projectPath)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(projectPath, testOutput);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync();
        await app.StopAsync();
    }

    [Theory]
    [MemberData(nameof(AppHostProjects))]
    public async Task HealthEndpointsReturnHealthy(string projectName, string projectPath)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(projectPath, testOutput);
        appHost.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(resilience =>
            {
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                resilience.Retry.MaxRetryAttempts = 10;
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
            });
        });
        var projects = appHost.Resources.OfType<ProjectResource>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        foreach (var project in projects)
        {
            if (!project.TryGetEndpoints(out var _))
            {
                // No endpoints so ignore this project
                continue;
            }

            using var client = app.CreateHttpClient(project.Name);
            HttpResponseMessage? response = null;

            try
            {
                response = await client.GetAsync("/health");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Error calling health endpoint for project '{project.GetName()}' in app '{Path.GetFileNameWithoutExtension(projectPath)}': {ex.Message}");
            }

            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Project isn't configured with health checks endpoint
                continue;
            }

            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Health endpoint for project '{project.GetName()}' in app '{Path.GetFileNameWithoutExtension(projectPath)}' returned status code {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }
    }

    public static object[][] AppHostProjects()
    {
        var samplesDir = Path.Combine(GetRepoRoot(), "samples");
        var appHostProjects = Directory.GetFiles(samplesDir, "*.AppHost.csproj", SearchOption.AllDirectories);
        return appHostProjects.Select(p => new object[] { Path.GetFileNameWithoutExtension(p), p }).ToArray();
    }

    private static string GetRepoRoot()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory != null && !File.Exists(Path.Combine(currentDirectory.FullName, "global.json")))
        {
            currentDirectory = currentDirectory.Parent;
        }

        if (currentDirectory == null)
        {
            throw new InvalidOperationException("Could not find the repository root.");
        }

        return currentDirectory.FullName;
    }
}
