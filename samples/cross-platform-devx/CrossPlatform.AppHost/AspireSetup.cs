using Aspire.Hosting.Azure;
using Projects;

namespace CrossPlatform.AppHost;

    /// <summary>
    /// There are a few Emulated resources that need specific images or setup configuration in order
    /// to execute properly on ARM environments
    /// </summary>
    /// <param name="builder"></param>
    public class AspireSetup(IDistributedApplicationBuilder builder)
    {
        public InitialisedResource Initialise()
        {
            var storage = builder
                .AddAzureStorage("storage")
                .RunAsEmulator((azurite => {
                    azurite.WithDataVolume();
                    azurite.WithLifetime(ContainerLifetime.Persistent);
                }));
           
            var queueResource = storage.AddQueues("queues");
            var blobResource = storage.AddBlobs("blobs");
            var container = storage.AddBlobContainer("blob-container", "blob-container");

            var cosmos = builder.AddAzureCosmosDB("cosmosdb");
            cosmos.AddCosmosDatabase(
                    "cosmosdb-database", "app")
                .AddContainer(
                    "cosmosdb-container", containerName: "tasks", partitionKeyPath: "/id");
            
            
            var serviceBus = builder.AddAzureServiceBus("messaging");
            var topic = serviceBus.AddServiceBusTopic("geographies");
            var subscription = topic.AddServiceBusSubscription("demo");

            var sql = builder.AddAzureSqlServer("sql");
            var database = sql.AddDatabase("database");
            
            return new InitialisedResource
            {
                ApplicationBuilder = builder,
                CosmosDbResource = cosmos,
                StorageResource = storage,
                ServiceBusResource = serviceBus,
                SqlDatabaseResource = database,
                SqlResource = sql,
                QueueResource = queueResource,
                BlobResource = blobResource,
                ContainerResource = container,
            };
        }
        

    #pragma warning disable ASPIRECOSMOSDB001
        public WireUp DoMacSetup(InitialisedResource resources)
        {
            resources.SqlResource.RunAsContainer(resourceBuilder =>
            {
                resourceBuilder
                    .WithImage("azure-sql-edge")
                    .WithImageTag("latest");
            });
            
            resources.CosmosDbResource.RunAsPreviewEmulator(configure =>
            {
                configure.WithDataExplorer();
                configure.WithGatewayPort(50754);
                configure.WithLifetime(ContainerLifetime.Persistent);
            });
            
            resources.ServiceBusResource.RunAsEmulator(sb =>
            {
                var edge = sb.ApplicationBuilder.Resources.OfType<ContainerResource>()
                    .First(resource => resource.Name.EndsWith("-mssql"));

                var annotation = edge.Annotations.OfType<ContainerImageAnnotation>().First();
                    
                annotation.Image = "azure-sql-edge";
                annotation.Tag = "latest";
            });
            
            return new WireUp(resources);
        }
    #pragma warning restore ASPIRECOSMOSDB001

        public WireUp DoWindowsSetup(InitialisedResource resources)
        {
            resources.CosmosDbResource.RunAsEmulator(configure =>
            {
            });

            resources.SqlResource.RunAsContainer();
            
            resources.ServiceBusResource.RunAsEmulator();
            
            return new WireUp(resources);
        }
        

        public class WireUp(InitialisedResource resources)
        {
            public void ThenWireUpTargets()
            {
                resources.ApplicationBuilder.AddProject<CrossPlatform_Web_Api>("api")
                    .WithOtlpExporter()
                    .WithReference(resources.CosmosDbResource)
                    .WithReference(resources.ServiceBusResource)
                    .WithReference(resources.SqlDatabaseResource!)
                    .WithReference(resources.ContainerResource)
                    .WithReference(resources.QueueResource)
                    .WaitFor(resources.ContainerResource)
                    .WaitFor(resources.CosmosDbResource)
                    .WaitFor(resources.ServiceBusResource)
                    .WaitFor(resources.SqlDatabaseResource!)
                    .WaitFor(resources.StorageResource)
                    .PublishAsAzureContainerApp((infra, app) => app.Configuration.Ingress.AllowInsecure = true);
            }
        }
        
        public class InitialisedResource
        {
            public required IDistributedApplicationBuilder ApplicationBuilder { get; set; }
            
            public required IResourceBuilder<AzureCosmosDBResource> CosmosDbResource { get; set; }
            
            public required IResourceBuilder<AzureStorageResource> StorageResource { get; set; }
            
            public required IResourceBuilder<AzureServiceBusResource> ServiceBusResource { get; set; }
            
            public required IResourceBuilder<AzureSqlServerResource> SqlResource { get; set; }
            public required IResourceBuilder<AzureQueueStorageResource> QueueResource { get; set; }
            public required IResourceBuilder<AzureBlobStorageResource> BlobResource { get; set; }
            public required IResourceBuilder<AzureSqlDatabaseResource>? SqlDatabaseResource { get; set; }
            public required IResourceBuilder<AzureBlobStorageContainerResource> ContainerResource { get; set; }
        }
    }
