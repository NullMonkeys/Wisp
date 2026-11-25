namespace Wisp.Gateway.Features.TempVoice;

public static class TempVoiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTempVoice()
        {
            services.AddSingleton<ITempVoiceDiscord, TempVoiceDiscord>();
            services.AddSingleton<ITempVoiceGuildState, TempVoiceGuildState>();

            services.AddScoped<ITempVoiceSettingsService, TempVoiceSettingsService>();
            services.AddScoped<ITempVoiceChannelsService, TempVoiceChannelsService>();

            return services;
        }
    }
}
