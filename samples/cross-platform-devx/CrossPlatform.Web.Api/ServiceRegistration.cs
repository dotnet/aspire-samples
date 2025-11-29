using System.Text.Json;
using CrossPlatform.Web.Api.Data;

namespace CrossPlatform.Web.Api;

public static class ServiceRegistration
{
    public static void RegisterAllServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<CosmosSeeder>();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        
        builder.AddAzureCosmosClient(connectionName: "cosmosdb", configureClientOptions: options =>
        {
            options.UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        });
        
        builder.AddAzureCosmosContainer("cosmosdb-container", settings => {});
        builder.AddSqlServerClient(connectionName: "database");
        builder.AddAzureBlobContainerClient("blob-container");
        builder.AddAzureQueueServiceClient("queues");
        builder.AddAzureServiceBusClient(connectionName: "messaging");
        
        builder.Services.AddCqrsServices();
    }
}

