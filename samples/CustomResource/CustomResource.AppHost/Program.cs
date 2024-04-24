var builder = DistributedApplication.CreateBuilder(args);
var maildev = builder.AddMailDev("maildev");
builder.AddProject<Projects.CustomResource_SampleApp>("customresource-sampleapp")
       .WithReference(maildev);

builder.Build().Run();
