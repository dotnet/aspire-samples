using Serilog;
using Serilog.Events;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddSerilog((_, loggerConfiguration) =>
{
    // Configure Serilog as desired here for AppHost logs (or use IConfiguration)
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Aspire.Hosting.Dcp", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var apiService = builder.AddProject<Projects.AspireWithSerilog_ApiService>("apiservice");

builder.AddProject<Projects.AspireWithSerilog_Web>("webfrontend")
    .WithReference(apiService);

builder.Build().Run();
