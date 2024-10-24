var builder = DistributedApplication.CreateBuilder(args);

builder.AddPythonApp("instrumented-python-app", "../InstrumentedPythonProject", "main.py")
       .WithEndpoint(scheme: "http", env: "PORT")
       .WithOtlpExporter();

builder.Build().Run();
