using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace SamplesIntegrationTests;

internal static class ResourceExtensions
{
    public static string GetName(this ProjectResource project)
    {
        var metadata = project.GetProjectMetadata();
        return Path.GetFileNameWithoutExtension(metadata.ProjectPath);
    }

    public static async Task<bool> TryApplyEfMigrationsAsync(this ProjectResource project, HttpClient httpClient)
    {
        // First check if the project has a migration endpoint, if it doesn't it will respond with a 404
        using var checkResponse = await httpClient.GetAsync("/ApplyDatabaseMigrations");
        if (checkResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        // Load the project assembly and find all DbContext types
        var projectName = Path.GetFileNameWithoutExtension(project.GetProjectMetadata().ProjectPath);
        var projectDirectory = Path.GetDirectoryName(project.GetProjectMetadata().ProjectPath) ?? throw new UnreachableException();
#if DEBUG
        var configuration = "Debug";
#else
        var configuration = "Release";
#endif
        var projectAssemblyPath = Path.Combine(projectDirectory, "bin", configuration, "net8.0", $"{projectName}.dll");
        var projectAssembly = Assembly.LoadFrom(projectAssemblyPath);
        var dbContextTypes = projectAssembly.GetTypes().Where(t => DerivesFromDbContext(t));

        // Call the migration endpoint for each DbContext type
        var migrationsApplied = false;
        foreach (var dbContextType in dbContextTypes)
        {
            using var content = new FormUrlEncodedContent([new("context", dbContextType.AssemblyQualifiedName)]);
            using var response = await httpClient.PostAsync("/ApplyDatabaseMigrations", content);
            migrationsApplied = migrationsApplied || response.StatusCode == HttpStatusCode.NoContent;
        }

        return migrationsApplied;
    }

    private static bool DerivesFromDbContext(Type type)
    {
        var baseType = type.BaseType;

        while (baseType is not null)
        {
            if (baseType.FullName == "Microsoft.EntityFrameworkCore.DbContext" && baseType.Assembly.GetName().Name == "Microsoft.EntityFrameworkCore")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}
