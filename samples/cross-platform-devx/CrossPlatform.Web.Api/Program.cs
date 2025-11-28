using CrossPlatform.Web.Api;
using CrossPlatform.Web.Api.EndpointHandling.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.RegisterAllServices();

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapHealthChecks("/health");

RouteGroupBuilder api = app.MapGroup("/api/v1");

api.UseGetEnrichedGeographiesEndpoint();

app.Run();

namespace CrossPlatform.Web.Api
{
    public class Program
    {
    }
}
