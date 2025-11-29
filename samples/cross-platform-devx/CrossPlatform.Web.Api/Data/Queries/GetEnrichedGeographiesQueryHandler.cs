using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CQRS.Mediatr.Lite;
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
        serviceBusClient.CreateSender("cosmosdb-container");
        var a = containerClient.AccountName;
        var b = queueServiceClient.AccountName;
        var x = primaryConnection.AccessToken;
        
        var container = client.GetContainer("app", "tasks");
        
        var query = "SELECT * FROM c";
        var iterator = container.GetItemQueryIterator<Geography>(query);
        List<Geography> allItems = [];

        while (iterator.HasMoreResults)
        {
            allItems.AddRange(await iterator.ReadNextAsync());
        }
        
        return allItems.ToList();
    }
}

