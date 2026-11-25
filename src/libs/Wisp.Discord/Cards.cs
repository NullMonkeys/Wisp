using NetCord;
using NetCord.Rest;

namespace Wisp.Discord;

public static class Cards
{
    public static ComponentContainerProperties Success(string title, string? body = null, string? footer = null)
        => Build(Color.Success, $"✅ {title}", body, footer);

    public static ComponentContainerProperties Warning(string title, string? body = null, string? footer = null)
        => Build(Color.Warning, $"⚠️ {title}", body, footer);

    public static ComponentContainerProperties Error(string title, string? body = null, string? footer = null)
        => Build(Color.Error, $"❌ {title}", body, footer);

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