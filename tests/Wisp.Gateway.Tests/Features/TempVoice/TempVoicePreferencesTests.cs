using NetCord;

using Wisp.Gateway.Features.TempVoice;
using Wisp.Gateway.Tests.Infrastructure;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Tests.Features.TempVoice;

public sealed class TempVoicePreferencesTests(DatabaseFixture database)
{
    [Fact]
    public async Task Renaming_saves_only_the_name()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var userId = TestIds.Next();
        var channelId = (await h.JoinAsync(userId, await h.EnableAndGetMasterAsync()))!.Value;
        var before = h.Discord.SettingsOf(channelId);

        // Act
        await h.SavePreferencesAsync(channelId, before, before with { Name = "Chill" });

        // Assert
        var preferences = await h.PreferencesAsync(userId);
        Assert.NotNull(preferences);
        Assert.Equal("Chill", preferences.Name);
        Assert.Null(preferences.UserLimit);
        Assert.Null(preferences.Bitrate);
        Assert.Empty(preferences.Overwrites);
    }

    [Fact]
    public async Task Later_edits_are_merged_into_existing_preferences()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var userId = TestIds.Next();
        var channelId = (await h.JoinAsync(userId, await h.EnableAndGetMasterAsync()))!.Value;
        var original = h.Discord.SettingsOf(channelId);
        var renamed = original with { Name = "Chill" };
        await h.SavePreferencesAsync(channelId, original, renamed);

        // Act
        await h.SavePreferencesAsync(channelId, renamed, renamed with { UserLimit = 4 });

        // Assert
        var preferences = await h.PreferencesAsync(userId);
        Assert.NotNull(preferences);
        Assert.Equal("Chill", preferences.Name);
        Assert.Equal(4, preferences.UserLimit);
    }

    [Fact]
    public async Task Overwrite_changes_save_only_what_differs_from_the_category()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var config = await h.EnableAsync();
        var hiddenRole = TestIds.Next();
        h.Discord.SetOverwrites(config.CategoryId!.Value,
            new PermissionOverwriteSnapshot(hiddenRole, PermissionOverwriteType.Role, default, Permissions.ViewChannel));
        var ownerId = TestIds.Next();
        var channelId = (await h.JoinAsync(ownerId, config.MasterChannelId!.Value))!.Value;
        var before = h.Discord.SettingsOf(channelId);
        var friend = new PermissionOverwriteSnapshot(TestIds.Next(), PermissionOverwriteType.User, Permissions.Connect, default);
        var mutedRole = new PermissionOverwriteSnapshot(TestIds.Next(), PermissionOverwriteType.Role, default, Permissions.Speak);

        // Act
        await h.SavePreferencesAsync(channelId, before, before with { Overwrites = [.. before.Overwrites, friend, mutedRole] });

        // Assert
        var preferences = await h.PreferencesAsync(ownerId);
        Assert.NotNull(preferences);

        // The owner's and bot's overwrites are the bot's own; the hidden role just repeats the category.
        var saved = preferences.Overwrites.Select(o => o.Id).ToHashSet();
        Assert.Equal(new[] { friend.Id, mutedRole.Id }.Order(), saved.Order());
        Assert.DoesNotContain(ownerId, saved);
        Assert.DoesNotContain(h.Discord.BotId, saved);
        Assert.DoesNotContain(hiddenRole, saved);

        var savedFriend = preferences.Overwrites.Single(o => o.Id == friend.Id);
        Assert.Equal(PermissionOverwriteKind.User, savedFriend.Kind);
        Assert.Equal((ulong)Permissions.Connect, savedFriend.Allowed);
    }

    [Fact]
    public async Task Saved_overwrites_survive_a_round_trip_into_the_next_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var ownerId = TestIds.Next();
        var channelId = (await h.JoinAsync(ownerId, master))!.Value;
        var before = h.Discord.SettingsOf(channelId);
        var friend = new PermissionOverwriteSnapshot(TestIds.Next(), PermissionOverwriteType.User, Permissions.Connect, default);
        await h.SavePreferencesAsync(channelId, before, before with { Name = "Den", Overwrites = [.. before.Overwrites, friend] });

        // Act
        await h.LeaveAsync(ownerId);
        var nextChannelId = (await h.JoinAsync(ownerId, master))!.Value;

        // Assert
        var next = h.Discord.Channel(nextChannelId)!;
        Assert.NotEqual(channelId, nextChannelId);
        Assert.Equal("Den", next.Name);
        Assert.Equal(friend, next.OverwriteFor(friend.Id));
    }

    [Fact]
    public async Task An_update_that_changes_nothing_user_editable_saves_nothing()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var userId = TestIds.Next();
        var channelId = (await h.JoinAsync(userId, await h.EnableAndGetMasterAsync()))!.Value;
        var settings = h.Discord.SettingsOf(channelId);

        // Act
        await h.SavePreferencesAsync(channelId, settings, settings with { });

        // Assert
        Assert.Null(await h.PreferencesAsync(userId));
    }

    [Fact]
    public async Task Editing_a_regular_channel_saves_nothing()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        await h.EnableAsync();
        var regular = new TempChannelSettings("General", 0, 64000, null, []);

        // Act
        await h.SavePreferencesAsync(TestIds.Next(), regular, regular with { Name = "Renamed" });

        // Assert
        Assert.Equal(0, await h.QueryAsync(db => Task.FromResult(db.TempVoicePreferences.Count(p => p.GuildId == h.GuildId))));
    }
}
