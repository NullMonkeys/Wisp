using NetCord;
using NetCord.Rest;

using Wisp.Discord.Components;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Features.JoinRoles.Modules;

internal static class JoinRolesCards
{
    public const string RolesMenuId = "join-roles-roles";
    public const string EnableId = "join-roles-enable";
    public const string DisableId = "join-roles-disable";

    public static ComponentContainerProperties Settings(
        JoinRolesConfig config,
        BotSnapshot? bot,
        IReadOnlyList<RejectedRole>? rejected = null)
    {
        List<IComponentContainerComponentProperties> components =
        [
            new TextDisplayProperties("### 🎭 Join roles"),
            Cards.StatusSection(
                config.Enabled,
                config.Enabled ? "**Status:** enabled" : "**Status:** disabled",
                EnableId,
                DisableId,
                canEnable: config.RoleIds.Count > 0),
            new TextDisplayProperties(config.RoleIds.Count == 0
                ? "No roles picked yet."
                : $"New members get: {string.Join(" ", config.RoleIds.Select(Mention))}"),
        ];

        if (bot is not { CanManageRoles: true })
            components.Add(new TextDisplayProperties("⚠️ I'm missing **Manage Roles**, so I can't give anyone these roles."));

        if (rejected is { Count: > 0 })
        {
            var lines = rejected.Select(r => $"- {Mention(r.RoleId)}: {Describe(r.Reason)}");
            components.Add(new TextDisplayProperties($"⚠️ Skipped:\n{string.Join("\n", lines)}"));
        }

        components.Add(new ComponentSeparatorProperties());
        components.Add(new RoleMenuProperties(RolesMenuId)
        {
            DefaultValues = config.RoleIds,
            MinValues = 0,
            MaxValues = JoinRolesConfig.MaxRoles,
            Placeholder = "Pick the roles new members get",
        });
        components.Add(new TextDisplayProperties(
            $"-# Up to {JoinRolesConfig.MaxRoles} roles. Bots are skipped, and members behind membership screening get them once they pass it."));

        return new ComponentContainerProperties
        {
            AccentColor = config.Enabled ? Color.Success : null,
            Components = components,
        };
    }

    private static string Mention(ulong roleId) => $"<@&{roleId}>";

    private static string Describe(RoleRejection reason) => reason switch
    {
        RoleRejection.Everyone => "everyone already has it",
        RoleRejection.Managed => "it's managed by an integration or bot",
        RoleRejection.AboveBot => "it's not below my highest role",
        RoleRejection.OverLimit => $"only {JoinRolesConfig.MaxRoles} roles are allowed",
        _ => "I can't find it",
    };
}
