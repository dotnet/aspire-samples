using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var pythonapp = builder.AddPythonApp("instrumented-python-app", "../InstrumentedPythonProject", "app.py")
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints()
       .WithOtlpExporter();

if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
{
    pythonapp.WithEnvironment("DEBUG", "True");
}

builder.Build().Run();
