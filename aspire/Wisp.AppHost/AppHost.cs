var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgWeb()
    .WithDataVolume("wisp-pg-data");

var database = postgres.AddDatabase("database", "wisp");

var restProxy = builder
    .AddContainer("rest-proxy", "ghcr.io/twilight-rs/http-proxy", "latest")
    .WithEnvironment("DISCORD_TOKEN", discordToken)
    .WithHttpEndpoint(targetPort: 80, name: "http");

builder
    .AddDotnetProject("gateway", "../../src/Wisp.Gateway")
    .WithHttpHealthCheck("/healthz")
    .WithEnvironment("Discord:Token", discordToken)
    .WithEnvironment("Discord:RestProxy", restProxy.GetEndpoint("http"))
    .WithReference(database)
    .WaitFor(database)
    .WaitFor(restProxy);

await builder.Build().RunAsync();