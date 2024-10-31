using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using ImageGallery.FrontEnd;
using ImageGallery.FrontEnd.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddAzureBlobClient("blobs");
builder.AddAzureQueueClient("queues");

builder.Services.AddSingleton<QueueMessageHandler>();
builder.Services.AddHostedService<StorageWorker>();

builder.Services.AddSingleton(
    static provider => provider.GetRequiredService<QueueServiceClient>().GetQueueClient("thumbnail-queue"));
builder.Services.AddKeyedSingleton(
    "images", static (provider, _) => provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("images"));
builder.Services.AddKeyedSingleton(
    "thumbnails", static (provider, _) => provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("thumbnails"));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
