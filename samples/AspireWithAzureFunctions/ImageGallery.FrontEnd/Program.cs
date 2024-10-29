using ImageGallery.FrontEnd;
using ImageGallery.FrontEnd.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Azure Storage
builder.AddAzureBlobClient("blobs");
builder.AddAzureQueueClient("queues");
builder.Services.AddSingleton<QueueMessageHandler>();
builder.Services.AddHostedService<StorageWorker>();

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
