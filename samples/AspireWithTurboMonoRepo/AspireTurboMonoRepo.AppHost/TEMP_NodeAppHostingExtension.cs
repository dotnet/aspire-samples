using Microsoft.Extensions.Hosting;

namespace AspireTurboMonoRepo.AppHost;

//
//
// NOTE - This file holds a temporary implementation of the AddGenericNodeApp() extension method.
//        It should be removed once the Aspire.Hosting package is updated to include the
//        AddGenericNodeApp() extension method as proposed in https://github.com/dotnet/aspire/pull/1448
//
//

public static class TEMP_NodeAppHostingExtension
{
    /// <summary>
    /// Adds a node application to the application model. Executes the pnpm command with the specified script name.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="workingDirectory">The working directory to use for the command. If null, the working directory of the current process is used.</param>
    /// <param name="packageManager">The package manager to use. Defaults to "npm".</param>
    /// <param name="scriptName">The npm script to execute. Defaults to "start".</param>
    /// <param name="args">The arguments to pass to the command.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NodeAppResource> AddGenericNodeApp(this IDistributedApplicationBuilder builder, string name, string workingDirectory, string packageManager = "npm", string scriptName = "start", string[]? args = null)
    {
        string[] allArgs = args is { Length: > 0 }
            ? ["run", scriptName, "--", .. args]
            : ["run", scriptName];

        var resource = new NodeAppResource(name, packageManager, workingDirectory);
        return builder.AddResource(resource)
            .WithNodeDefaults()
            .WithArgs(allArgs);
    }
    
    private static IResourceBuilder<NodeAppResource> WithNodeDefaults(this IResourceBuilder<NodeAppResource> builder) =>
        builder.WithOtlpExporter()
            .WithEnvironment("NODE_ENV", builder.ApplicationBuilder.Environment.IsDevelopment() ? "development" : "production");

}
