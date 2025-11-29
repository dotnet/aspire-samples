using CQRS.Mediatr.Lite;
using CrossPlatform.Web.Api.Data.Model;
using CrossPlatform.Web.Api.Data.Queries;

namespace CrossPlatform.Web.Api.Data;

public static class CqrsModule
{
    public static IServiceCollection AddCqrsServices(this IServiceCollection services)
    {
        // Basic CQRS Services from underlying library
        services.AddTransient<IQueryService, QueryService>();
        services.AddTransient<IRequestHandlerResolver>(ctx => new RequestHandlerResolver(ctx.GetRequiredService));
        
        // This project specific queries 
        services.AddTransient<GetEnrichedGeographiesQueryHandler>();
        services.AddTransient<QueryHandler<GetEnrichedGeographiesQuery, IEnumerable<Geography>>>(sp => 
            sp.GetRequiredService<GetEnrichedGeographiesQueryHandler>());

        return services;
    }
}

