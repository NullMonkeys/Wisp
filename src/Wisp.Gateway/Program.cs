using System.Reflection;

using NetCord.Hosting.Services;

using Wisp.Discord;
using Wisp.Gateway.Features.Guilds;
using Wisp.Gateway.Features.JoinRoles;
using Wisp.Gateway.Features.TempVoice;
using Wisp.Persistence;
using Wisp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddPersistence();
builder.AddDiscordGateway();

builder.Services.AddGuilds();
builder.Services.AddTempVoice();
builder.Services.AddJoinRoles();

var app = builder.Build();

app.AddModules(Assembly.GetExecutingAssembly());

await app.MigrateDatabaseAsync();
await app.RunAsync();
