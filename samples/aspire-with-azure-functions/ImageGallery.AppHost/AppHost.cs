using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var storage = builder.AddAzureStorage("storage").RunAsEmulator()
    .ConfigureInfrastructure((infrastructure) =>
    {
        var storageAccount = infrastructure.GetProvisionableResources().OfType<StorageAccount>().FirstOrDefault(r => r.BicepIdentifier == "storage")
            ?? throw new InvalidOperationException($"Could not find configured storage account with name 'storage'");

        // Ensure that public access to blobs is disabled
        storageAccount.AllowBlobPublicAccess = false;
    })
    .WithUrls(c =>
    {
        // None of the URLs are usable in the browser so hide them from the summary page
        foreach (var url in c.Urls)
        {
            url.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }
    });
var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");

var functions = builder.AddAzureFunctionsProject<Projects.ImageGallery_Functions>("functions")
                       .WithReference(queues)
                       .WithReference(blobs)
                       .WaitFor(storage)
                       .WithRoleAssignments(storage,
                            // Storage Account Contributor and Storage Blob Data Owner roles are required by the Azure Functions host
                            StorageBuiltInRole.StorageAccountContributor, StorageBuiltInRole.StorageBlobDataOwner,
                            // Queue Data Contributor role is required to send messages to the queue
                            StorageBuiltInRole.StorageQueueDataContributor)
                       .WithHostStorage(storage)
                       .WithUrlForEndpoint("http", u => u.DisplayText = "Functions App");

builder.AddProject<Projects.ImageGallery_FrontEnd>("frontend")
       .WithReference(queues)
       .WithReference(blobs)
       .WaitFor(functions)
       .WithExternalHttpEndpoints()
       .WithUrlForEndpoint("https", u => u.DisplayText = "Frontend App")
       .WithUrlForEndpoint("http", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly);

builder.Build().Run();
