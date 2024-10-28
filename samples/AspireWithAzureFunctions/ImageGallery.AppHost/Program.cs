var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");

builder.AddProject<Projects.ImageGallery_Web>("frontend")
       .WithReference(queues)
       .WithReference(blobs)
       .WaitFor(storage);

builder.AddAzureFunctionsProject<Projects.ImageGalleryFunctions>("imagegalleryfunctions")
       .WithReference(queues)
       .WithReference(blobs)
       .WaitFor(storage);

builder.Build().Run();
