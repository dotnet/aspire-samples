var builder = DistributedApplication.CreateBuilder(args);

builder.AddPythonApp("instrumented-python-app", "../InstrumentedPythonProject", "main.py")
       .WithHttpEndpoint(env: "PORT")
       .WithOtlpExporter();

builder.Build().Run();
