var builder = DistributedApplication.CreateBuilder(args);

// Define our microservices
var profile = builder.AddProject<Projects.ProfileMicroservice>("profile");
var feed = builder.AddProject<Projects.FeedMicroservice>("feed");

// Define our gateway
var apiGateway = builder.AddProject<Projects.YarpApiGateway>("apigateway")
    .WithReference(profile)
    .WithReference(feed);

// Define our clients
var publicSite = builder.AddProject<Projects.BlazorPublicSite>("publicsite")
    .WithReference(apiGateway);


var publicSiteBetaVersion = builder.AddProject<Projects.BlazorPublicSiteBetaVersion>("publicsitebetaversion")
    .WithReference(apiGateway);


// Define the ingress for the clients
builder.AddProject<Projects.YarpIngress>("ingress")
    .WithReference(publicSite)
    .WithReference(publicSiteBetaVersion);


builder.Build().Run();
