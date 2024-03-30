using System.Diagnostics;
using System.Net;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

public class AppHostTests(ITestOutputHelper testOutput)
{
    [Theory]
    [MemberData(nameof(AppHostProjects))]
    public async Task AppHostRunsCleanly(string projectFile)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(GetProjectPath(projectFile), testOutput);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync(waitForResourcesToStart: true);

        Assert.DoesNotContain(app.GetResourceLogs(), entry => entry.Key is ProjectResource or ExecutableResource && entry.Value.Any(log => log.IsErrorMessage));
        //app.EnsureNoResourceErrors();

        await app.StopAsync();
    }

    [Theory]
    [MemberData(nameof(AppHostProjects))]
    public async Task HealthEndpointsReturnHealthy(string projectFile)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(GetProjectPath(projectFile), testOutput);
        //appHost.Services.ConfigureHttpClientDefaults(http =>
        //{
        //    http.AddStandardResilienceHandler(resilience =>
        //    {
        //        resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
        //        resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        //        resilience.Retry.MaxRetryAttempts = 10;
        //        resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
        //    });
        //});
        var projects = appHost.Resources.OfType<ProjectResource>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync(waitForResourcesToStart: true);

        foreach (var project in projects)
        {
            if (!project.TryGetEndpoints(out var _))
            {
                // No endpoints so ignore this project
                continue;
            }

            using var client = app.CreateHttpClient(project.Name);

            await project.TryApplyEfMigrationsAsync(client);

            HttpResponseMessage? response = null;

            try
            {
                response = await client.GetAsync("/health");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Error calling health endpoint for project '{project.GetName()}' in app '{Path.GetFileNameWithoutExtension(projectFile)}': {ex.Message}");
            }

            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Project isn't configured with health checks endpoint
                continue;
            }

            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Health endpoint for project '{project.GetName()}' in app '{Path.GetFileNameWithoutExtension(projectFile)}' returned status code {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }

        await app.StopAsync();
    }

    public static object[][] AppHostProjects()
    {
        var repoRoot = GetRepoRoot();
        var samplesDir = Path.Combine(repoRoot, "samples");
        var appHostProjects = Directory.GetFiles(samplesDir, "*.AppHost.csproj", SearchOption.AllDirectories);
        return appHostProjects.Select(p => new object[] { Path.GetRelativePath(repoRoot, p) }).ToArray();
    }

    private static string GetProjectPath(string projectFile)
    {
        return Path.GetFullPath(projectFile, GetRepoRoot());
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
