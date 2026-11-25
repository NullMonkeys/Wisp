var builder = DistributedApplication.CreateBuilder(args);

var discordToken = builder.AddParameter("discord-token", secret: true);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgWeb()
    .WithDataVolume("wisp-pg-data");

var database = postgres.AddDatabase("database", "wisp");

builder
    .AddDotnetProject("gateway", "../../src/Wisp.Gateway")
    .WithHttpHealthCheck("/healthz")
    .WithEnvironment("Discord:Token", discordToken)
    .WithReference(database)
    .WaitFor(database);

await builder.Build().RunAsync();