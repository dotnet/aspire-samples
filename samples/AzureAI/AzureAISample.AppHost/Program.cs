var builder = DistributedApplication.CreateBuilder(args);

// Azure OpenAI creates deployments for each model version. This is the name of the deployment to use.
var deploymentName = "mygpt4";
var openai = builder.AddAzureOpenAI("openai")
    .AddDeployment(
        new(deploymentName, "gpt-4o", "2024-05-13")
    );

builder.AddProject<Projects.AzureAISample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(openai)
    .WithEnvironment("AI_DeploymentName", deploymentName);

builder.Build().Run();
