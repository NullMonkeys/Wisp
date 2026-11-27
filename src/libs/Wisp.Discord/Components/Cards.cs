using NetCord;
using NetCord.Rest;

namespace Wisp.Discord.Components;

public static class Cards
{
    public static ComponentContainerProperties Success(string title, string? body = null, string? footer = null)
        => Build(Color.Success, $"✅ {title}", body, footer);

    public static ComponentContainerProperties Warning(string title, string? body = null, string? footer = null)
        => Build(Color.Warning, $"⚠️ {title}", body, footer);

    public static ComponentContainerProperties Error(string title, string? body = null, string? footer = null)
        => Build(Color.Error, $"❌ {title}", body, footer);

    /// <summary>
    /// A status line with an Enable or Disable button beside it, depending on <paramref name="enabled"/>.
    /// </summary>
    /// <remarks>
    /// Enable and Disable get separate custom IDs rather than one toggle, so pressing Disable on a stale card still
    /// means disable.
    /// </remarks>
    public static ComponentSectionProperties StatusSection(
        bool enabled,
        string status,
        string enableId,
        string disableId,
        bool canEnable = true)
    {
        var button = enabled
            ? new ButtonProperties(disableId, "Disable", ButtonStyle.Danger)
            : new ButtonProperties(enableId, "Enable", ButtonStyle.Success) { Disabled = !canEnable };

        return new ComponentSectionProperties(button, [new TextDisplayProperties(status)]);
    }

    private static ComponentContainerProperties Build(Color accent, string title, string? body, string? footer)
    {
        List<IComponentContainerComponentProperties> components = [new TextDisplayProperties($"### {title}")];

        if (body is not null)
        {
            components.Add(new TextDisplayProperties(body));
        }

        if (footer is not null)
        {
            components.Add(new ComponentSeparatorProperties());
            components.Add(new TextDisplayProperties($"-# {footer}"));
        }

        return new ComponentContainerProperties { AccentColor = accent, Components = components, };
    }

    extension(ComponentContainerProperties card)
    {
        public InteractionMessageProperties ToMessage(bool ephemeral = true) => new()
        {
            Flags = ephemeral ? MessageFlags.IsComponentsV2 | MessageFlags.Ephemeral : MessageFlags.IsComponentsV2,
            Components = [card],
        };
    }
}