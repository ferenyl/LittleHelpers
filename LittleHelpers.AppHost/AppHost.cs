var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker-compose");

var postgres = builder.AddPostgres("postgres")
    .WithEndpoint(name: "postgresendpoint", scheme: "tcp", port: 5432, targetPort: 5432, isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .AddDatabase("littlehelpers");

var apiService = builder.AddProject<Projects.LittleHelpers_ApiService>("apiservice")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithHttpHealthCheck("/health");

builder.AddJavaScriptApp("webfrontend", "../LittleHelpers.Web/ClientApp")
    .WithHttpEndpoint(port: 4500, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("services__apiservice__http__0", apiService.GetEndpoint("http"))
    .WaitFor(apiService);

builder.Build().Run();
