var builder = DistributedApplication.CreateBuilder(args);

var idpRealmName = "AspireKeycloakSample";

var idp = builder.AddKeycloak("idp")
    .WithDataVolume()
    .WithHttpsDevCertificate()
    //.WithRealmImport("realms", isReadOnly: true)
    ;

var apiService = builder.AddProject<Projects.Keycloak_ApiService>("apiservice")
    .WithReference(idp)
    .WithEnvironment(nameof(idpRealmName), idpRealmName);

builder.AddProject<Projects.Keycloak_Web_BlazorSSR>("web-blazorssr")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(idp)
    .WithEnvironment("idpClientId", "keycloak.web.blazorssr")
    .WithEnvironment("idpClientSecret", "67TXVfKutEcIOVtKTmiz201RntcFZgwK")
    .WithEnvironment(nameof(idpRealmName), idpRealmName);

builder.Build().Run();
