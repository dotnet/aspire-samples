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
           
            var queueResource = storage.AddQueue("queue");
            var blobResource = storage.AddBlobs("blobs");
            storage.AddBlobContainer("blob-container");

            var cosmos = builder.AddAzureCosmosDB("cosmosdb");
            cosmos.AddCosmosDatabase("cosmosdb-database", "app")
                .AddContainer("cosmosdb-container", containerName: "tasks", partitionKeyPath: "/id");
            
            var serviceBus = builder.AddAzureServiceBus("messaging");

            var sql = builder.AddAzureSqlServer("sql");
            
            return new InitialisedResource
            {
                ApplicationBuilder = builder,
                CosmosDbResource = cosmos,
                StorageResource = storage,
                ServiceBusResource = serviceBus,
                SqlResource = sql,
                QueueResource = queueResource,
                BlobResource = blobResource,
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

            resources.SqlResource.AddDatabase("database");
            
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
                    .WithReference(resources.SqlResource)
                    .WithReference(resources.BlobResource)
                    .WithReference(resources.QueueResource)
                    .WaitFor(resources.CosmosDbResource)
                    .WaitFor(resources.ServiceBusResource)
                    .WaitFor(resources.SqlResource)
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
            public required IResourceBuilder<AzureQueueStorageQueueResource> QueueResource { get; set; }
            public required IResourceBuilder<AzureBlobStorageResource> BlobResource { get; set; }
        }
    }
