var builder = DistributedApplication.CreateBuilder(args);

// Http based azure function
builder.AddProject<Projects.HttpFunctionApp>("http-based-azure-function");

// Timer based azure function
builder.AddProject<Projects.TimerTriggerFunctionApp>("timer-based-azure-function");

builder.Build().Run();
