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

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });
        
        builder.AddAzureCosmosClient("cosmosdb");
        builder.AddAzureCosmosContainer("cosmosdb-container");
        builder.AddSqlServerClient(connectionName: "database");
        builder.AddAzureBlobContainerClient("blob-container");
        builder.AddAzureQueueServiceClient("queues");
        builder.AddAzureServiceBusClient(connectionName: "messaging");
        
        builder.Services.AddCqrsServices();
    }
}

