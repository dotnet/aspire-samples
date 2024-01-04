
namespace OrchardCore.Mvc.HelloWorld;

public class Startup : Modules.StartupBase
{
    public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        routes.MapAreaControllerRoute
        (
            name: "Home",
            areaName: "OrchardCore.Mvc.HelloWorld",
            pattern: string.Empty,
            defaults: new { controller = "Home", action = "Index" }
        );
    }
}
