using System.Net;

using Microsoft.EntityFrameworkCore;

using NetCord;
using NetCord.Rest;

using Wisp.Gateway.Tests.Infrastructure;

namespace Wisp.Gateway.Tests.Features.TempVoice;

public sealed class TempVoiceSettingsTests(DatabaseFixture database)
{
    [Fact]
    public async Task Enable_creates_category_and_master_channel_and_saves_config()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);

        // Act
        var config = await h.EnableAsync();

        // Assert
        Assert.True(config.Enabled);

        var category = h.Discord.Channel(config.CategoryId!.Value);
        Assert.NotNull(category);
        Assert.Equal(ChannelType.CategoryChannel, category.Type);

        var master = h.Discord.Channel(config.MasterChannelId!.Value);
        Assert.NotNull(master);
        Assert.Equal(ChannelType.VoiceGuildChannel, master.Type);
        Assert.Equal(category.Id, master.ParentId);

        var saved = await h.GetConfigAsync();
        Assert.True(saved.Enabled);
        Assert.Equal(config.CategoryId, saved.CategoryId);
        Assert.Equal(config.MasterChannelId, saved.MasterChannelId);
    }

    [Fact]
    public async Task Enable_when_already_enabled_returns_existing_config_without_creating_anything()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var first = await h.EnableAsync();

        // Act
        var second = await h.EnableAsync();

        // Assert
        Assert.Equal(first.CategoryId, second.CategoryId);
        Assert.Equal(first.MasterChannelId, second.MasterChannelId);
        Assert.Equal(2, h.Discord.CreatedCount);
    }

    [Fact]
    public async Task Enable_when_master_channel_was_deleted_rebuilds_the_setup()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var broken = await h.EnableAsync();
        h.Discord.DeleteChannel(broken.MasterChannelId!.Value);

        // Act
        var repaired = await h.EnableAsync();

        // Assert
        Assert.True(repaired.Enabled);
        Assert.NotNull(h.Discord.Channel(repaired.MasterChannelId!.Value));
        Assert.NotNull(h.Discord.Channel(repaired.CategoryId!.Value));
        Assert.Null(h.Discord.Channel(broken.CategoryId!.Value));
    }

    [Fact]
    public async Task Enable_with_force_replaces_channels_and_removes_existing_temp_channels()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var old = await h.EnableAsync();
        var tempChannelId = await h.JoinAsync(TestIds.Next(), old.MasterChannelId!.Value);

        // Act
        var replaced = await h.EnableAsync(force: true);

        // Assert
        Assert.NotEqual(old.CategoryId, replaced.CategoryId);
        Assert.NotEqual(old.MasterChannelId, replaced.MasterChannelId);
        Assert.Null(h.Discord.Channel(old.CategoryId!.Value));
        Assert.Null(h.Discord.Channel(old.MasterChannelId!.Value));
        Assert.Null(h.Discord.Channel(tempChannelId!.Value));
        Assert.Empty(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Enable_when_master_channel_creation_fails_removes_the_category_and_saves_nothing()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        h.Discord.FailNextCreate(new RestException(HttpStatusCode.Forbidden, "Missing Permissions"),
            when: c => c.Type == ChannelType.VoiceGuildChannel);

        // Act
        var exception = await Record.ExceptionAsync(() => h.EnableAsync());

        // Assert
        Assert.IsType<RestException>(exception);
        Assert.Empty(h.Discord.ChannelsIn(h.GuildId));
        Assert.False((await h.GetConfigAsync()).Enabled);
    }

    [Fact]
    public async Task Enable_when_saving_fails_removes_the_created_channels()
    {
        // Arrange
        // No guild row, so saving the config violates its foreign key.
        await using var h = await TempVoiceHarness.CreateAsync(database, seedGuild: false);

        // Act
        var exception = await Record.ExceptionAsync(() => h.EnableAsync());

        // Assert
        Assert.IsType<DbUpdateException>(exception, exactMatch: false);
        Assert.Equal(2, h.Discord.CreatedCount);
        Assert.Empty(h.Discord.ChannelsIn(h.GuildId));
    }

    [Fact]
    public async Task Concurrent_enables_create_a_single_setup()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);

        // Act
        var configs = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => h.EnableAsync()));

        // Assert
        Assert.Equal(2, h.Discord.CreatedCount);
        Assert.Single(configs.Select(c => c.MasterChannelId).Distinct());
    }

    [Fact]
    public async Task Disable_removes_temp_channels_master_channel_and_category()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        await h.JoinAsync(TestIds.Next(), master);

        // Act
        var config = await h.DisableAsync();

        // Assert
        Assert.False(config.Enabled);
        Assert.Null(config.CategoryId);
        Assert.Null(config.MasterChannelId);
        Assert.Empty(h.Discord.ChannelsIn(h.GuildId));
        Assert.Empty(await h.TempChannelsAsync());
        Assert.False((await h.GetConfigAsync()).Enabled);
    }

    [Fact]
    public async Task Disable_when_channels_were_already_deleted_by_hand_still_succeeds()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var enabled = await h.EnableAsync();
        h.Discord.DeleteChannel(enabled.CategoryId!.Value);

        // Act
        var config = await h.DisableAsync();

        // Assert
        Assert.False(config.Enabled);
        Assert.Empty(h.Discord.ChannelsIn(h.GuildId));
    }

    [Fact]
    public async Task Disable_when_never_enabled_does_nothing()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);

        // Act
        var config = await h.DisableAsync();

        // Assert
        Assert.False(config.Enabled);
        Assert.Equal(0, h.Discord.CreatedCount);
    }

    [Fact]
    public async Task Deleting_the_guild_removes_its_config_temp_channels_and_preferences()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var userId = TestIds.Next();
        await h.JoinAsync(userId, await h.EnableAndGetMasterAsync());
        await h.SeedPreferencesAsync(new() { GuildId = h.GuildId, UserId = TestIds.Next(), Name = "Mine" });

        // Act
        await h.DeleteGuildAsync();

        // Assert
        Assert.False((await h.GetConfigAsync()).Enabled);
        Assert.Empty(await h.TempChannelsAsync());
        Assert.Equal(0, await h.QueryAsync(db => db.TempVoicePreferences.CountAsync(p => p.GuildId == h.GuildId,
            TestContext.Current.CancellationToken)));
    }
}
