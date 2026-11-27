namespace Wisp.Gateway.Features.JoinRoles;

public interface IJoinRolesGuildState
{
    RoleSnapshot? GetRole(
        ulong guildId,
        ulong roleId);

    /// <returns><c>null</c> if the guild or the bot's member isn't cached.</returns>
    BotSnapshot? GetBot(
        ulong guildId);
}
