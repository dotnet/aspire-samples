using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator()
    .ConfigureConstruct((ResourceModuleConstruct construct) =>
    {
        var storageAccount = construct.GetResources().OfType<StorageAccount>().FirstOrDefault(r => r.IdentifierName == "storage")
            ?? throw new InvalidOperationException($"Could not find configured storage account with name 'storage'");
        // Storage Account Contributor and Storage Blob Data Owner roles are required by the Azure Functions host
        construct.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageAccountContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
        construct.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageBlobDataOwner, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
        // Anonymous access to Blobs required by the Blazor front-end
        storageAccount.AllowBlobPublicAccess = true;
    });
var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");

var functions = builder.AddAzureFunctionsProject<Projects.ImageGallery_Functions>("functions")
                       .WithReference(queues)
                       .WithReference(blobs)
                       .WaitFor(storage)
                       .WithHostStorage(storage);

builder.AddProject<Projects.ImageGallery_FrontEnd>("frontend")
       .WithReference(queues)
       .WithReference(blobs)
       .WaitFor(functions)
       .WithExternalHttpEndpoints();

builder.Build().Run();
