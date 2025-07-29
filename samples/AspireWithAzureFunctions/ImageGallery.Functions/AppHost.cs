using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureQueueServiceClient("queues");
builder.AddAzureBlobServiceClient("blobs");

builder.ConfigureFunctionsWebApplication();

builder.Build().Run();
