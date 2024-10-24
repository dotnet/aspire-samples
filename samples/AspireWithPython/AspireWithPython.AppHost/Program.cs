var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddPythonApp("instrumented-python-app", "../InstrumentedPythonProject", "main.py")
       .WithEndpoint(scheme: "http", env: "PORT")
       .WithOtlpExporter();
#pragma warning restore ASPIREHOSTINGPYTHON001

builder.Build().Run();
