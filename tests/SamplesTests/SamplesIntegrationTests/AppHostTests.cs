using System.Net;
using System.Reflection;
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
        resourceLogs.EnsureNoErrors(resource => resource is ProjectResource or ExecutableResource);

        await app.StopAsync();
    }

    [Theory]
    [MemberData(nameof(AppHostsAssembliesWithTestEndpoints))]
    public async Task TestEndpointsReturnOk(string appHostPath, IReadOnlyDictionary<string, IReadOnlyList<string>> testEndpoints)
    {
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

            if (endpoints.Count == 0)
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
        resourceLogs.EnsureNoErrors(resource => resource is ProjectResource or ExecutableResource);

        await app.StopAsync();
    }

    public static object[][] AppHostAssemblies()
    {
        var appHostAssemblies = GetSamplesAppHostAssemblyPaths();
        return appHostAssemblies.Select(p => new object[] { Path.GetRelativePath(AppContext.BaseDirectory, p) }).ToArray();
    }

    public static object[][] AppHostsAssembliesWithTestEndpoints()
    {
        var appHostAssemblies = GetSamplesAppHostAssemblyPaths();
        var result = new List<object[]>();

        foreach (var appHost in appHostAssemblies)
        {
            var appHostAssembly = Assembly.LoadFrom(Path.Combine(appHost)) ?? throw new InvalidOperationException($"Could not load AppHost assembly '{appHost}'");
            var typeName = appHostAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "TestEndpointsTypeName")?.Value;
            var methodName = appHostAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "TestEndpointsMethodName")?.Value;

            if (!string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(methodName))
            {
                var type = appHostAssembly.GetType(typeName);
                var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException($"Could not find a public static method named '{methodName}' on type '{typeName}' in assembly {appHost}");
                if (!method.ReturnType.IsAssignableTo(typeof(IReadOnlyDictionary<string, IReadOnlyList<string>>)))
                {
                    throw new InvalidOperationException($"TestEndpoints method '{methodName}' on type '{typeName}' in assembly {appHost} must return a type assignable to IReadOnlyDictionary<string, IReadOnlyList<string>>");
                }
                var testEndpoints = method.Invoke(null, null) as IReadOnlyDictionary<string, IReadOnlyList<string>>
                    ?? throw new InvalidOperationException($"TestEndpoints method '{methodName}' on type '{typeName}' in assembly {appHost} returned null");
                
                result.Add([Path.GetRelativePath(AppContext.BaseDirectory, appHost), testEndpoints]);
            }
        }

        return [.. result];
    }

    private static IEnumerable<string> GetSamplesAppHostAssemblyPaths()
    {
        return Directory.GetFiles(AppContext.BaseDirectory, "*.AppHost.dll")
            .Where(fileName => !fileName.EndsWith("Aspire.Hosting.AppHost.dll", StringComparison.OrdinalIgnoreCase)
                               // Known issue with Dapr in preview.5 and randomization of resource names that occurs in integration testing
                               && !fileName.EndsWith("AspireWithDapr.AppHost.dll", StringComparison.OrdinalIgnoreCase));
    }
}
