namespace Wisp.Gateway.Features.JoinRoles;

public static class JoinRolesExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJoinRoles()
        {
            services.AddSingleton<IJoinRolesDiscord, JoinRolesDiscord>();
            services.AddSingleton<IJoinRolesGuildState, JoinRolesGuildState>();

            services.AddScoped<IJoinRolesSettingsService, JoinRolesSettingsService>();
            services.AddScoped<IJoinRolesMembersService, JoinRolesMembersService>();

            return services;
        }
    }
}
