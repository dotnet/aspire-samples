using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]

namespace SamplesIntegrationTests;

public class AppHostTests
{
    [Theory]
    [MemberData(nameof(AppHostProjectPaths))]
    public async Task AppHostProjectLaunchesAndShutsDownCleanly(string projectPath)
    {
        var appHost = await CreateDistributedApplicationBuilder(projectPath);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync();
        await app.StopAsync();
    }

    [Theory]
    [MemberData(nameof(AppHostProjectPaths))]
    public async Task ProjectResourcesHealthEndpointsReturnHealthy(string projectPath)
    {
        var appHost = await CreateDistributedApplicationBuilder(projectPath);
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
                Assert.Fail($"Error calling health endpoint for project '{GetProjectName(project)}' in app '{Path.GetFileNameWithoutExtension(projectPath)}': {ex.Message}");
            }

            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Project isn't configured with health checks endpoint
                continue;
            }

            Assert.True(HttpStatusCode.OK == response.StatusCode, $"Health endpoint for project '{GetProjectName(project)}' in app '{Path.GetFileNameWithoutExtension(projectPath)}' returned status code {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }
    }

    public static object[][] AppHostProjectPaths()
    {
        var samplesDir = Path.Combine(GetRepoRoot(), "samples");
        var appHostProjects = Directory.GetFiles(samplesDir, "*.AppHost.csproj", SearchOption.AllDirectories);
        return appHostProjects.Select(p => new object[] { p }).ToArray();
    }

    private static async Task<IDistributedApplicationTestingBuilder> CreateDistributedApplicationBuilder(string appHostProjectPath)
    {
        var appHostProjectName = Path.GetFileNameWithoutExtension(appHostProjectPath) ?? throw new InvalidOperationException("AppHost project was not found.");
        var appHostProjectDirectory = Path.GetDirectoryName(appHostProjectPath) ?? throw new InvalidOperationException("Directory for AppHost project was not found.");
#if DEBUG
        var configuration = "Debug";
#else
        var configuration = "Release";
#endif
        // TODO: Handle different assets output path
        var appHostAssembly = Assembly.LoadFrom(Path.Combine(appHostProjectDirectory, "bin", configuration, "net8.0", $"{appHostProjectName}.dll"));

        var appHostType = appHostAssembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith("_AppHost"))
            ?? throw new InvalidOperationException("Generated AppHost type not found.");

        var createAsyncMethod = typeof(DistributedApplicationTestingBuilder).GetMethod(nameof(DistributedApplicationTestingBuilder.CreateAsync))
            ?? throw new InvalidOperationException("DistributedApplicationTestingBuilder.CreateAsync method not found.");

        var createAsyncConcrete = createAsyncMethod.MakeGenericMethod(appHostType);

        var testBuilderTask = createAsyncConcrete.Invoke(null, [CancellationToken.None]) as Task<IDistributedApplicationTestingBuilder>
            ?? throw new UnreachableException();

        var builder = await testBuilderTask;

        return builder;
    }

    private static string GetProjectName(ProjectResource project)
    {
        var metadata = project.GetProjectMetadata();
        return Path.GetFileNameWithoutExtension(metadata.ProjectPath);
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
