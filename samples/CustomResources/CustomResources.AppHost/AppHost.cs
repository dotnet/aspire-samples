using CustomResources.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddTalkingClock("talking-clock");

builder.AddTestResource("test");

builder.Build().Run();
