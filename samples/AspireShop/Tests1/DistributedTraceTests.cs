using Microsoft.Extensions.DependencyInjection;
using Aspire.Dashboard;
using Aspire.Dashboard.Model;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using System.Reflection;
using Aspire.Dashboard.Otlp.Model;
using System.Text;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace SamplesIntegrationTests;

public static class OtlpTraceExtensions
{
    private static readonly List<string> NecessaryKeys = new List<string>
    {
        "Name",
        //"Kind",
        //"http.request.method",
        "http.response.status_code",
        //"http.route",
        //"network.protocol.version",
        //"server.address",
        //"server.port",
        //"url.path",
        //"url.scheme",
        "db.user: postgres",
        "db.name: catalogdb",
        "rpc.grpc.status_code",
        "grpc.method",
        //"url.full",
        //"db.connection_string",
        "db.statement",
    };

    public static Dictionary<string, string> FilterByNecessaryKeys(this Dictionary<string, string> dictionary)
    {
        return dictionary
            .Where(kvp => NecessaryKeys.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static string PrintTree(this OtlpTrace trace)
    {
        var rootSpan = trace.Spans.FirstOrDefault(span => string.IsNullOrEmpty(span.ParentSpanId));
        if (rootSpan == null) return "No Root Span Found";

        var sb = new StringBuilder();
        trace.PrintSpanTree(sb, rootSpan, "", true);
        return sb.ToString();
    }

    private static void PrintSpanTreeAlternative(this OtlpTrace trace, StringBuilder sb, OtlpSpan span, string indent, bool last)
    {
        var indentCount = span.AllProperties().Keys.Count + 2;

        sb.Append(indent);
        if (last)
        {
            sb.Append("╚══");
            indent += "    ";
        }
        else
        {
            sb.Append("╠══");
            indent += "║   ";
        }

        print(sb, span, indent);

        var children = trace.Spans.Where(s => s.ParentSpanId == span.SpanId).ToList();
        for (int i = 0; i < children.Count; i++)
        {
            trace.PrintSpanTreeAlternative(sb, children[i], indent, i == children.Count - 1);
        }

        static void print(StringBuilder sb, OtlpSpan span, string indent)
        {
            var p = span.AllProperties();
            var endpointName = p.GetValueOrDefault("Name");
            var status = p.GetValueOrDefault("http.response.status_code") ?? p.GetValueOrDefault("rpc.grpc.status_code");
            var sqlStatement = Inline(p?.GetValueOrDefault("db.statement") ?? "");
            sb.AppendLine($"{span.Kind.GetSpanEmoji()} {System.Enum.GetName(span.Kind.GetType(), span.Kind)} '{span.Source.ApplicationName.Split("_")[0]}' Calls {endpointName} with status {status}, {sqlStatement}");
        }
    }

    private static void PrintSpanTree(this OtlpTrace trace, StringBuilder sb, OtlpSpan span, string indent, bool last)
    {
        var indentCount = span.AllProperties().Keys.Count + 2;

        sb.Append(indent);
        if (last)
        {
            sb.Append("╚══");
            indent += "    ";
        }
        else
        {
            sb.Append("╠══");
            indent += "║   ";
        }

        sb.AppendLine($"{span.Kind.GetSpanEmoji()} {Enum.GetName(span.Kind.GetType(), span.Kind)} '{span.Source.ApplicationName.Split("_")[0]}'"); //other: { JsonSerializer.Serialize(span)}

        // Append each property in the dictionary
        foreach (var property in span.AllProperties().FilterByNecessaryKeys())
        {
            sb.AppendLine($"{indent}║ {property.Key}: {Inline(property.Value)}");
        }

        var children = trace.Spans.Where(s => s.ParentSpanId == span.SpanId).ToList();
        for (int i = 0; i < children.Count; i++)
        {
            trace.PrintSpanTree(sb, children[i], indent, i == children.Count - 1);
        }
    }

    public static string Inline(string text)
    {
        return text.Replace("\r\n", "  ").Replace("\n", "  ");
    }

    public static string GetSpanEmoji(this OtlpSpanKind kind)
    {
        return kind switch
        {
            OtlpSpanKind.Internal => "🔧",
            OtlpSpanKind.Server => "🌐",
            OtlpSpanKind.Client => "📤",
            OtlpSpanKind.Producer => "📦",
            OtlpSpanKind.Consumer => "📥",
            _ => "❓"
        };
    }
}


public class MyServiceConfigurator
{
    public void Configure(IServiceCollection services)
    {
    }
}
public static class ServiceCollectionExtensions
{
    public static TService GetService<TService>(this IServiceProvider provider, Type implementationType) where TService : class
    {
        if (provider is null)
            throw new ArgumentNullException(nameof(provider));

        if (implementationType is null)
            throw new ArgumentNullException(nameof(implementationType));

        var service = provider.GetServices<TService>()
                              .FirstOrDefault(s => s.GetType() == implementationType);

        if (service == null)
            throw new InvalidOperationException($"No service for type '{typeof(TService)}' and implementation '{implementationType}' has been registered.");

        return service;
    }
}
public static class DashboardWebApplicationExtensions
{
    public static IServiceProvider GetAppServices(this DashboardWebApplication dashboardWebApplication)
    {
        // Use reflection to get the private _app field
        FieldInfo appField = typeof(DashboardWebApplication).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance);
        if (appField == null)
        {
            throw new InvalidOperationException("The DashboardWebApplication class does not contain a private field named '_app'.");
        }

        // Get the _app instance from the dashboardWebApplication instance
        WebApplication appInstance = appField.GetValue(dashboardWebApplication) as WebApplication;
        if (appInstance == null)
        {
            throw new InvalidOperationException("Unable to retrieve the WebApplication instance from the DashboardWebApplication instance.");
        }

        // Return the IServiceProvider from the WebApplication instance
        return appInstance.Services;
    }
}
public static class IDistributedApplicationTestingBuilderExtensions
{
    public static IServiceCollection AddDashboardWebApplication(this IServiceCollection services)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var myServiceConfigurator = new MyServiceConfigurator();
        var logger = loggerFactory.CreateLogger<DashboardWebApplication>();
        services.AddTransient(sp => logger);
        services.AddTransient<Action<IServiceCollection>>(sp => myServiceConfigurator.Configure);
        services.AddHostedService<DashboardWebApplication>();
        return services;
    }

    public static IServiceProvider GetDashboardWebApplication(this IServiceProvider services)
    {
        var dashboardWebApplication = (DashboardWebApplication)services.GetService<IHostedService>(typeof(DashboardWebApplication));
        var serviceProvider = dashboardWebApplication.GetAppServices();
        return serviceProvider;
    }

    public static async Task<string> CaptureTraces(this TracesViewModel tracesViewModel, Func<Task> func)
    {
        await Task.Delay(3000);// Padding some time to isolate the function under the test.
        var dateTimeStart = DateTime.Now;
        await func();
        var dateTimeEnd = DateTime.Now;
        await Task.Delay(3000);

        var traces = tracesViewModel?.GetTraces();
        var filteredTraces = traces.Items.Where(item => item?.RootSpan?.StartTime.ToLocalTime().Ticks >= dateTimeStart.Ticks && item.RootSpan?.StartTime.ToLocalTime().Ticks <= dateTimeEnd.Ticks).ToList();

        StringBuilder t = new StringBuilder();
        foreach (var trace in filteredTraces)
        {
            var text = trace.PrintTree();
            t.AppendLine(text);
        }

        var tree = t.ToString();

        return tree;
    }
}
public class DistributedTracesTests()
{
    [Fact]
    public async Task EndpointCall_ShouldCaptureDistributedTracesAsASnapshot()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireShop_AppHost>();

        appHost.Services.AddDashboardWebApplication();

        await using var app = await appHost.BuildAsync();

        await app.StartAsync();

        var tracesViewModel = app.Services.GetDashboardWebApplication().GetService<TracesViewModel>();

        var httpClient = app.CreateHttpClient("frontend");

        var tracesTree = await tracesViewModel.CaptureTraces(async () =>
        {
            var response = await httpClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });

        await app.StopAsync();

        await Verify(tracesTree);
        //Produces file
        //╚══🌐 Server 'frontend'
        //    ║ Name: GET /
        //    ║ http.response.status_code: 200
        //    ╠══📤 Client 'frontend'
        //    ║   ║ Name: GET
        //    ║   ║ http.response.status_code: 200
        //    ║   ╚══🌐 Server 'catalogservice'
        //    ║       ║ Name: GET /api/v1/catalog/items/type/all/brand/{catalogBrandId?}
        //    ║       ║ http.response.status_code: 200
        //    ║       ╚══📤 Client 'catalogservice'
        //    ║           ║ Name: catalogdb
        //    ║           ║ db.statement: SELECT c."Id", c."AvailableStock", c."CatalogBrandId", c."CatalogTypeId", c."Description", c."MaxStockThreshold", c."Name", c."OnReorder", c."PictureFileName", c."Price", c."RestockThreshold"  FROM "Catalog" AS c  ORDER BY c."Id"  LIMIT @__pageSize + 1
        //    ╚══📤 Client 'frontend'
        //        ║ Name: BasketApi.Basket/GetBasketById
        //        ║ rpc.grpc.status_code: 0
        //        ╚══📤 Client 'frontend'
        //            ║ Name: POST
        //            ║ http.response.status_code: 200
        //            ╚══🌐 Server 'basketservice'
        //                ║ Name: POST /BasketApi.Basket/GetBasketById
        //                ║ grpc.method: /BasketApi.Basket/GetBasketById
        //                ║ http.response.status_code: 200
    }

    [Fact]
    public async Task EndpointCall_ShouldGenerateDistributedTraces()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireShop_AppHost>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // getting aspire TracesViewModel service, but tracesViewModel is null ?
        var tracesViewModel = app.Services.GetService<TracesViewModel>();
        //how can i get aspire TracesViewModel service?

        // Act
        var httpClient = app.CreateHttpClient("frontend");
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(tracesViewModel?.GetTraces()?.Items);

        await app.StopAsync();
    }

    [Fact]
    public async Task EndpointCall_ShouldGenerateDistributedTracesWorks()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireShop_AppHost>();
        appHost.Services.AddDashboardWebApplication();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var tracesViewModel = app.Services.GetDashboardWebApplication().GetService<TracesViewModel>();

        // Act
        var httpClient = app.CreateHttpClient("frontend");
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(tracesViewModel.GetTraces().Items);

        await app.StopAsync();
    }

}
