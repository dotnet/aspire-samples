using System.Data.Common;
using Aspire.Hosting.Utils;

var builder = DistributedApplication.CreateBuilder(args);

var mysql = builder.AddMySqlContainer("sql");
mysql.WithEnvironment("MYSQL_DATABASE", "wpdb");

builder.AddWordPressContainer("wp").WithReference(mysql);

// This can go away once this issue is resolved: https://github.com/Azure/azure-dev/issues/3141
builder.AddProject<Projects.DummyApp>("dummyapp");

builder.Build().Run();

public static class WordPressBuilderExtensions
{
    public static IResourceBuilder<WordPressResource> AddWordPressContainer(this IDistributedApplicationBuilder builder, string name)
    {
        var wordpress = new WordPressResource(name);
        return builder.AddResource(wordpress)
            .WithAnnotation(new ContainerImageAnnotation() { Image = "wordpress", Tag = "latest" })
            .WithServiceBinding(80, scheme: "http");
    }

    public static IResourceBuilder<WordPressResource> WithReference(this IResourceBuilder<WordPressResource> builder, IResourceBuilder<MySqlContainerResource> database)
    {
        return builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables.Add("WORDPRESS_DB_HOST", $"{{{database.Resource.Name}.bindings.tcp.host}}:{{{database.Resource.Name}.bindings.tcp.port}}");
                context.EnvironmentVariables.Add("WORDPRESS_DB_USER", "root");
                context.EnvironmentVariables.Add("WORDPRESS_DB_PASSWORD", $"{{{database.Resource.Name}.inputs.password}}");
                context.EnvironmentVariables.Add("WORDPRESS_DB_NAME", $"wpdb");
            }
            else
            {
                var connectionStringBuilder = new DbConnectionStringBuilder();
                connectionStringBuilder.ConnectionString = database.Resource.GetConnectionString();

                // We use this here because in the case of WordPress we are translating
                var resolvedHostName = HostNameResolver.ReplaceLocalhostWithContainerHost((string)connectionStringBuilder["server"], builder.ApplicationBuilder.Configuration);

                context.EnvironmentVariables.Add("WORDPRESS_DB_HOST", $"{resolvedHostName}:{connectionStringBuilder["port"]}");
                context.EnvironmentVariables.Add("WORDPRESS_DB_USER", (string)connectionStringBuilder["user id"]);
                context.EnvironmentVariables.Add("WORDPRESS_DB_PASSWORD", (string)connectionStringBuilder["password"]);
                context.EnvironmentVariables.Add("WORDPRESS_DB_NAME", (string)connectionStringBuilder["database"]);
            }

        });
    }
}

public class WordPressResource(string name) : ContainerResource(name)
{
}
