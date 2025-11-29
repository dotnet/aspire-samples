using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CQRS.Mediatr.Lite;
using CrossPlatform.Web.Api.Data.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;

namespace CrossPlatform.Web.Api.Data.Queries;

public class GetEnrichedGeographiesQueryHandler(
    CosmosClient client,
    ServiceBusClient serviceBusClient,
    SqlConnection primaryConnection,
    BlobContainerClient containerClient,
    QueueServiceClient queueServiceClient
    )
    : QueryHandler<GetEnrichedGeographiesQuery, IEnumerable<Geography>>
{

    protected override async Task<IEnumerable<Geography>> ProcessRequest(GetEnrichedGeographiesQuery request)
    {
        // Query Cosmos DB for geographies
        var container = client.GetContainer("app", "tasks");
        var query = "SELECT * FROM c";
        var iterator = container.GetItemQueryIterator<Geography>(query);
        List<Geography> allItems = [];

        while (iterator.HasMoreResults)
        {
            allItems.AddRange(await iterator.ReadNextAsync());
        }
        
        // Publish a message to Service Bus
        await using var serviceBusSender = serviceBusClient.CreateSender("geographies");
        var serviceBusMessage = new ServiceBusMessage($"Processed {allItems.Count} geographies at {DateTime.UtcNow:O}");
        await serviceBusSender.SendMessageAsync(serviceBusMessage);
        
        // Write a blob to Azure Blob Storage
        var blobName = $"geographies-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var blobClient = containerClient.GetBlobClient(blobName);
        var blobContent = System.Text.Json.JsonSerializer.Serialize(allItems);
        await blobClient.UploadAsync(new BinaryData(blobContent), overwrite: true);
        
        // Add a message to Azure Queue Storage
        var queueClient = queueServiceClient.GetQueueClient("queues");
        await queueClient.CreateIfNotExistsAsync();
        await queueClient.SendMessageAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            QueryId = request.Id,
            ProcessedCount = allItems.Count,
            Timestamp = DateTime.UtcNow
        }));
        
        // Use SQL Connection to log processing
        await primaryConnection.OpenAsync();
        await using var sqlCommand = new SqlCommand(
            "SELECT GETUTCDATE() AS CurrentTime", 
            primaryConnection);
        var sqlResult = await sqlCommand.ExecuteScalarAsync();
        await primaryConnection.CloseAsync();
        
        return allItems.ToList();
    }
}

