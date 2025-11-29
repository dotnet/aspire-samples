using CrossPlatform.Web.Api.Contracts.Response;
using CrossPlatform.Web.Api.Data.Queries;

namespace CrossPlatform.Web.Api.EndpointHandling.Endpoints;

public static class MapperExtensions
{
    public static List<EnrichedGeographyResponse> MapToResponse(this IEnumerable<Geography> geographies)
    {
        return new List<EnrichedGeographyResponse>();
    }
}
