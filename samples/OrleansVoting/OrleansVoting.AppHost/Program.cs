var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("voting-redis");
var orleans = builder.AddOrleans("voting-cluster")
    .WithClustering(redis)
    .WithGrainStorage("votes", redis);

builder.AddProject<Projects.OrleansVoting_Service>("voting-fe")
    .WithReference(orleans)
    .WithReplicas(3);

builder.Build().Run();
