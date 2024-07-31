var builder = DistributedApplication.CreateBuilder(args);

builder.AddPythonProject("instrumented-python-project", "../InstrumentedPythonProject", "main.py")
       .WithEndpoint(scheme: "http", env: "PORT");

builder.Build().Run();
