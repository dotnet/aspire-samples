using System.Runtime.InteropServices;
using Projects;

#pragma warning disable ASPIRECOSMOSDB001

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var storage = builder
    .AddAzureStorage("storage")
    .RunAsEmulator((azurite => {
        azurite.WithDataVolume();
        azurite.WithLifetime(ContainerLifetime.Persistent);
    }));
           
var queueResource = storage.AddQueues("queues");
storage.AddBlobs("blobs");
var container = storage.AddBlobContainer("blob-container", "blob-container");

var cosmos = builder.AddAzureCosmosDB("cosmosdb");
cosmos.AddCosmosDatabase(
        "cosmosdb-database", "app")
    .AddContainer(
        "cosmosdb-container", containerName: "tasks", partitionKeyPath: "/id");
            
var serviceBus = builder.AddAzureServiceBus("messaging");
var topic = serviceBus.AddServiceBusTopic("geographies");
topic.AddServiceBusSubscription("demo");

var sql = builder.AddAzureSqlServer("sql");
var database = sql.AddDatabase("database");

    
if (OperatingSystem.IsMacOS() && RuntimeInformation.OSArchitecture == Architecture.Arm64)
{
    sql.RunAsContainer(resourceBuilder =>
    {
        // Needs this for ARM? Other images gave errors re AMD/ARM mismatch, containers unhealthy
        resourceBuilder
            .WithImage("azure-sql-edge")
            .WithImageTag("latest");
    });
            
    cosmos.RunAsPreviewEmulator(configure =>
    {
        configure.WithDataExplorer();
        configure.WithGatewayPort(50754);
        configure.WithLifetime(ContainerLifetime.Persistent);
    });
            
    serviceBus.RunAsEmulator(sb =>
    {
        var edge = sb.ApplicationBuilder.Resources.OfType<ContainerResource>()
            .First(resource => resource.Name.EndsWith("-mssql"));

        var annotation = edge.Annotations.OfType<ContainerImageAnnotation>().First();
                    
        annotation.Image = "azure-sql-edge";
        annotation.Tag = "latest";
    });

}
else if (OperatingSystem.IsWindows() && RuntimeInformation.OSArchitecture == Architecture.X64)
{
    cosmos.RunAsEmulator(configure =>
    {
    });

    sql.RunAsContainer();
            
    serviceBus.RunAsEmulator();
}
else
{
    Console.WriteLine("¯\\_(ツ)_/¯ left as an exercise for the reader");
}

builder.AddProject<CrossPlatform_Web_Api>("api")
    .WithOtlpExporter()
    .WithReference(cosmos)
    .WithReference(serviceBus)
    .WithReference(database)
    .WithReference(container)
    .WithReference(queueResource)
    .WaitFor(container)
    .WaitFor(cosmos)
    .WaitFor(serviceBus)
    .WaitFor(database)
    .WaitFor(storage)
    .PublishAsAzureContainerApp((infra, app) => app.Configuration.Ingress.AllowInsecure = true);

builder.Build().Run();
Console.WriteLine("Exit");
