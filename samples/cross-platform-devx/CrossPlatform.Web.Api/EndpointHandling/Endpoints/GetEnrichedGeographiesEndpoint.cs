using CQRS.Mediatr.Lite;
using CrossPlatform.Web.Api.Contracts.Response;
using CrossPlatform.Web.Api.Data.Queries;
using Microsoft.AspNetCore.Mvc;

namespace CrossPlatform.Web.Api.EndpointHandling.Endpoints;

internal static class GetEnrichedGeographiesEndpoint
{
    internal static IEndpointRouteBuilder UseGetEnrichedGeographiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/enriched-geographies",
                async (
                    [FromServices] IQueryService queryHandler,
                    CancellationToken ct
                ) =>
                {
                    GetEnrichedGeographiesQuery query = new();
                    IEnumerable<Geography>? resultFromQuery = await queryHandler.Query(query);

                    return resultFromQuery.ToList();
                })
            .Produces<EnrichedGeographyResponse[]>(StatusCodes.Status200OK, "application/json")
            .WithName("GetEnrichedGeographies")
            .WithOpenApi();

        return endpoints;
    }
}

