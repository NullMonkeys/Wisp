using NetCord.Rest;

using Wisp.Gateway.Features.JoinRoles;
using Wisp.Gateway.Features.JoinRoles.Commands;
using Wisp.Gateway.Features.TempVoice.Commands;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Tests.Features;

public sealed class SettingsCardsTests
{
    private static readonly BotSnapshot Bot = new(TopRolePosition: 100, CanManageRoles: true);

    [Fact]
    public void Temp_voice_card_offers_enable_while_disabled()
    {
        var card = TempVoiceCards.Settings(new TempVoiceConfig { GuildId = 1 }, activeChannels: 0);

        var button = ButtonOf(card);
        Assert.Equal(TempVoiceCards.EnableId, button.CustomId);
        Assert.False(button.Disabled);
    }

    [Fact]
    public void Temp_voice_card_offers_disable_and_shows_the_count_while_enabled()
    {
        var config = new TempVoiceConfig { GuildId = 1, Enabled = true, CategoryId = 2, MasterChannelId = 3 };

        var card = TempVoiceCards.Settings(config, activeChannels: 3);

        Assert.Equal(TempVoiceCards.DisableId, ButtonOf(card).CustomId);
        Assert.Contains("3 active channels", TextOf(card));
        Assert.Contains("<#3>", TextOf(card));
    }

    [Fact]
    public void Join_roles_card_greys_out_enable_without_roles()
    {
        var card = JoinRolesCards.Settings(new JoinRolesConfig { GuildId = 1 }, Bot);

        var button = ButtonOf(card);
        Assert.Equal(JoinRolesCards.EnableId, button.CustomId);
        Assert.True(button.Disabled);
    }

    [Fact]
    public void Join_roles_card_allows_enable_with_roles()
    {
        var card = JoinRolesCards.Settings(new JoinRolesConfig { GuildId = 1, RoleIds = [5] }, Bot);

        var button = ButtonOf(card);
        Assert.Equal(JoinRolesCards.EnableId, button.CustomId);
        Assert.False(button.Disabled);
    }

    [Fact]
    public void Join_roles_card_offers_disable_and_preselects_the_roles_while_enabled()
    {
        var card = JoinRolesCards.Settings(new JoinRolesConfig { GuildId = 1, Enabled = true, RoleIds = [5, 6] }, Bot);

        Assert.Equal(JoinRolesCards.DisableId, ButtonOf(card).CustomId);
        var menu = Assert.Single(card.Components.OfType<RoleMenuProperties>());
        Assert.Equal([5UL, 6UL], menu.DefaultValues);
        Assert.Equal(JoinRolesConfig.MaxRoles, menu.MaxValues);
    }

    private static ButtonProperties ButtonOf(ComponentContainerProperties card)
    {
        var section = Assert.Single(card.Components.OfType<ComponentSectionProperties>());
        return Assert.IsType<ButtonProperties>(section.Accessory);
    }

    private static string TextOf(ComponentContainerProperties card)
        => string.Join("\n", card.Components
            .SelectMany(c => c is ComponentSectionProperties s ? s.Components.Cast<object>() : [c])
            .OfType<TextDisplayProperties>()
            .Select(t => t.Content));
}
