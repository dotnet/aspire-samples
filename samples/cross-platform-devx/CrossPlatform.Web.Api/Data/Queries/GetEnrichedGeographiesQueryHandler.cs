using CQRS.Mediatr.Lite;
using Microsoft.Azure.Cosmos;

namespace CrossPlatform.Web.Api.Data.Queries;

public class GetEnrichedGeographiesQueryHandler(CosmosClient client)
    : QueryHandler<GetEnrichedGeographiesQuery, IEnumerable<Geography>>
{

    protected override async Task<IEnumerable<Geography>> ProcessRequest(GetEnrichedGeographiesQuery request)
    {
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

