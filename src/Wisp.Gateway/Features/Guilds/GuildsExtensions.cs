namespace Wisp.Gateway.Features.Guilds;

public static class GuildsExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGuilds()
        {
            services.AddScoped<IGuildsService, GuildsService>();
            return services;
        }
    }
}
