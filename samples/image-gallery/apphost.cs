#pragma warning disable ASPIRECSHARPAPPS001
#pragma warning disable ASPIREAZURE002

#:sdk Aspire.AppHost.Sdk@13.6.0
#:package Aspire.Hosting.Azure.Storage@13.6.0
#:package Aspire.Hosting.Azure.Sql@13.6.0
#:package Aspire.Hosting.JavaScript@13.6.0
#:package Aspire.Hosting.Azure.AppContainers@13.6.0

using Aspire.Hosting.Azure;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

// Storage: Use Azurite emulator in run mode, real Azure in publish mode
var storage = builder.AddAzureStorage("storage")
                .RunAsEmulator();

var blobs = storage.AddBlobContainer("images");
var queues = storage.AddQueues("queues");

// Azure SQL Database
var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer(c => c.WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase("imagedb");

// API: Upload images, queue thumbnail jobs, serve metadata
var api = builder.AddCSharpApp("api", "./api")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WaitFor(sql)
    .WithReference(blobs)
    .WithReference(queues)
    .WithReference(sql)
    .WithUrls(context =>
    {
        foreach (var u in context.Urls)
        {
            u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }

        context.Urls.Add(new()
        {
            Url = "/scalar",
            DisplayText = "API Reference",
            Endpoint = context.GetEndpoint("https")
        });
    })
    .PublishAsAzureContainerApp((infra, app) =>
    {
        // Scale to zero when idle
        app.Template.Scale.MinReplicas = 0;
    });

// Worker: Container Apps Job for queue-triggered thumbnail generation
// Event-driven: starts when messages arrive, exits within ~5 seconds when queue is empty
var worker = builder.AddCSharpApp("worker", "./worker")
    .WithReference(blobs)
    .WithReference(queues)
    .WithReference(sql)
    .WaitFor(sql)
    .WaitFor(queues);

if (builder.ExecutionContext.IsRunMode)
{
    // In run mode, keep worker running continuously for fast local development
    worker = worker.WithEnvironment("WORKER_RUN_CONTINUOUSLY", "true");
}
else
{
    // In publish mode, use event-driven scaling based on queue depth
    worker.PublishAsAzureContainerAppJob((infra, job) =>
    {
        var accountNameParameter = queues.Resource.Parent.NameOutputReference.AsProvisioningParameter(infra);

        // Resolve the identity annotation added to the worker app
        if (!worker.Resource.TryGetLastAnnotation<AppIdentityAnnotation>(out var identityAnnotation))
        {
            throw new InvalidOperationException("Identity annotation not found.");
        }

        job.Configuration.TriggerType = ContainerAppJobTriggerType.Event;
        job.Configuration.EventTriggerConfig.Scale.PollingIntervalInSeconds = 1;
        job.Configuration.EventTriggerConfig.Scale.Rules.Add(new ContainerAppJobScaleRule
        {
            Name = "queue-rule",
            JobScaleRuleType = "azure-queue",
            Metadata = new ObjectExpression(
                new PropertyExpression("accountName", new IdentifierExpression(accountNameParameter.BicepIdentifier)),
                new PropertyExpression("queueName", new StringLiteralExpression("thumbnails")),
                new PropertyExpression("queueLength", new IntLiteralExpression(1))
            ),
            Identity = identityAnnotation.IdentityResource.Id.AsProvisioningParameter(infra)
        });
    });
}

// Frontend: Vite+React for upload and gallery UI
var frontend = builder.AddViteApp("frontend", "./frontend")
    .WithEndpoint("http", e => e.Port = 9080)
    .WithReference(api)
    .WithUrl("", "Image Gallery");

// Publish: Embed frontend build output in API container
api.PublishWithContainerFiles(frontend, "wwwroot");

builder.Build().Run();
