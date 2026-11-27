using System.Net;

using NetCord;
using NetCord.Rest;

using Wisp.Gateway.Features.TempVoice;
using Wisp.Gateway.Tests.Infrastructure;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Tests.Features.TempVoice;

public sealed class TempVoiceChannelsTests(DatabaseFixture database)
{
    [Fact]
    public async Task Joining_the_master_channel_creates_a_temp_channel_and_moves_the_user_into_it()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var config = await h.EnableAsync();
        var userId = TestIds.Next();

        // Act
        var channelId = await h.JoinAsync(userId, config.MasterChannelId!.Value, displayName: "Alice");

        // Assert
        Assert.NotNull(channelId);
        Assert.NotEqual(config.MasterChannelId, channelId);

        var channel = h.Discord.Channel(channelId.Value);
        Assert.NotNull(channel);
        Assert.Equal("Alice's channel", channel.Name);
        Assert.Equal(ChannelType.VoiceGuildChannel, channel.Type);
        Assert.Equal(config.CategoryId, channel.ParentId);

        var row = Assert.Single(await h.TempChannelsAsync());
        Assert.Equal(channelId, row.ChannelId);
        Assert.Equal(userId, row.OwnerId);
    }

    [Fact]
    public async Task Temp_channel_gives_the_owner_management_rights_and_keeps_the_categorys_overwrites()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var config = await h.EnableAsync();
        var membersRole = TestIds.Next();
        h.Discord.SetOverwrites(config.CategoryId!.Value,
            new PermissionOverwriteSnapshot(membersRole, PermissionOverwriteType.Role, Permissions.Connect, default));
        var userId = TestIds.Next();

        // Act
        var channelId = await h.JoinAsync(userId, config.MasterChannelId!.Value);

        // Assert
        var channel = h.Discord.Channel(channelId!.Value)!;

        var owner = channel.OverwriteFor(userId);
        Assert.NotNull(owner);
        Assert.True(owner.Value.Allowed.HasFlag(Permissions.ManageChannels));
        Assert.True(owner.Value.Allowed.HasFlag(Permissions.ManageRoles));
        Assert.True(owner.Value.Allowed.HasFlag(Permissions.MoveUsers));

        Assert.NotNull(channel.OverwriteFor(h.Discord.BotId));

        var inherited = channel.OverwriteFor(membersRole);
        Assert.NotNull(inherited);
        Assert.Equal(Permissions.Connect, inherited.Value.Allowed);
    }

    [Fact]
    public async Task Active_count_follows_temp_channels_being_created_and_emptied()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var alice = TestIds.Next();
        await h.JoinAsync(alice, master);
        await h.JoinAsync(TestIds.Next(), master);

        // Act
        var whileBothActive = await h.CountActiveAsync();
        await h.LeaveAsync(alice);

        // Assert
        Assert.Equal(2, whileBothActive);
        Assert.Equal(1, await h.CountActiveAsync());
    }

    [Fact]
    public async Task Joining_a_regular_channel_does_nothing()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        await h.EnableAsync();
        var regular = TestIds.Next();

        // Act
        var channelId = await h.JoinAsync(TestIds.Next(), regular);

        // Assert
        Assert.Equal(regular, channelId);
        Assert.Equal(2, h.Discord.CreatedCount);
        Assert.Empty(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Joining_the_old_master_channel_after_disable_does_nothing()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        await h.DisableAsync();

        // Act
        await h.JoinAsync(TestIds.Next(), master);

        // Assert
        Assert.Empty(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Joining_the_master_channel_again_moves_the_owner_back_to_their_existing_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var userId = TestIds.Next();
        var first = await h.JoinAsync(userId, master);
        await h.JoinAsync(TestIds.Next(), first!.Value); // someone stays, so the channel survives the owner leaving
        await h.LeaveAsync(userId);

        // Act
        var second = await h.JoinAsync(userId, master);

        // Assert
        Assert.Equal(first, second);
        Assert.Single(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Joining_the_master_channel_when_the_owned_channel_is_gone_creates_a_new_one()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var userId = TestIds.Next();
        var stale = await h.JoinAsync(userId, master);
        h.Discord.DeleteChannel(stale!.Value); // deleted while we weren't looking: the row is left behind

        // Act
        var fresh = await h.JoinAsync(userId, master);

        // Assert
        Assert.NotEqual(stale, fresh);
        var row = Assert.Single(await h.TempChannelsAsync());
        Assert.Equal(fresh, row.ChannelId);
    }

    [Fact]
    public async Task Saved_preferences_are_applied_to_the_new_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var config = await h.EnableAsync();
        var userId = TestIds.Next();
        var blockedRole = TestIds.Next();
        await h.SeedPreferencesAsync(new TempVoicePreferences
        {
            GuildId = h.GuildId,
            UserId = userId,
            Name = "Gaming",
            UserLimit = 5,
            Bitrate = 96000,
            Overwrites =
            [
                new TempVoiceOverwrite
                {
                    Id = blockedRole,
                    Kind = PermissionOverwriteKind.Role,
                    Denied = (ulong)Permissions.Connect,
                },
            ],
        });

        // Act
        var channelId = await h.JoinAsync(userId, config.MasterChannelId!.Value);

        // Assert
        var channel = h.Discord.Channel(channelId!.Value)!;
        Assert.Equal("Gaming", channel.Name);
        Assert.Equal(5, channel.UserLimit);
        Assert.Equal(96000, channel.Bitrate);

        var blocked = channel.OverwriteFor(blockedRole);
        Assert.NotNull(blocked);
        Assert.Equal(Permissions.Connect, blocked.Value.Denied);
    }

    [Fact]
    public async Task When_discord_rejects_saved_preferences_the_channel_is_created_with_defaults()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var userId = TestIds.Next();
        await h.SeedPreferencesAsync(new TempVoicePreferences { GuildId = h.GuildId, UserId = userId, Bitrate = 384000 });
        h.Discord.FailNextCreate(new RestException(HttpStatusCode.BadRequest, "Bad Request"), when: c => c.Bitrate == 384000);

        // Act
        var channelId = await h.JoinAsync(userId, master, displayName: "Alice");

        // Assert
        var channel = h.Discord.Channel(channelId!.Value)!;
        Assert.Equal("Alice's channel", channel.Name);
        Assert.Null(channel.Bitrate);
        Assert.Single(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task When_the_user_leaves_before_being_moved_the_channel_is_removed()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        h.Discord.FailMoves = true;

        // Act
        await h.JoinAsync(TestIds.Next(), master);

        // Assert
        Assert.Equal(3, h.Discord.CreatedCount);
        Assert.Equal(2, h.Discord.ChannelsIn(h.GuildId).Count); // only the category and the master channel remain
        Assert.Empty(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Concurrent_joins_by_the_same_user_create_a_single_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var userId = TestIds.Next();
        h.Discord.Connect(h.GuildId, userId, master);

        // Act
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => h.JoinAsync(userId, master)));

        // Assert
        Assert.Equal(3, h.Discord.CreatedCount);
        Assert.Single(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Last_user_leaving_deletes_the_temp_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var userId = TestIds.Next();
        var channelId = await h.JoinAsync(userId, await h.EnableAndGetMasterAsync());

        // Act
        await h.LeaveAsync(userId);

        // Assert
        Assert.Null(h.Discord.Channel(channelId!.Value));
        Assert.Empty(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Leaving_while_others_remain_keeps_the_temp_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var ownerId = TestIds.Next();
        var channelId = await h.JoinAsync(ownerId, await h.EnableAndGetMasterAsync());
        await h.JoinAsync(TestIds.Next(), channelId!.Value);

        // Act
        await h.LeaveAsync(ownerId);

        // Assert
        Assert.NotNull(h.Discord.Channel(channelId.Value));
        Assert.Single(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Leaving_a_regular_channel_does_nothing()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        await h.EnableAsync();
        var regular = TestIds.Next();
        var userId = TestIds.Next();
        await h.JoinAsync(userId, regular);

        // Act
        await h.LeaveAsync(userId);

        // Assert
        Assert.Equal(2, h.Discord.ChannelsIn(h.GuildId).Count);
    }

    [Fact]
    public async Task Forget_channel_removes_temp_channel_rows_only()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var channelId = await h.JoinAsync(TestIds.Next(), await h.EnableAndGetMasterAsync());

        // Act
        var forgotRegular = await h.ForgetChannelAsync(TestIds.Next());
        var forgotTemp = await h.ForgetChannelAsync(channelId!.Value);

        // Assert
        Assert.False(forgotRegular);
        Assert.True(forgotTemp);
        Assert.Empty(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task Reconcile_forgets_missing_channels_deletes_empty_ones_and_keeps_occupied_ones()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        (ulong gone, ulong emptied, ulong occupied) = (TestIds.Next(), TestIds.Next(), TestIds.Next());
        var goneChannel = await h.JoinAsync(gone, master);
        var emptiedChannel = await h.JoinAsync(emptied, master);
        var occupiedChannel = await h.JoinAsync(occupied, master);
        // What happened while the bot was offline:
        h.Discord.DeleteChannel(goneChannel!.Value);
        h.Discord.Disconnect(h.GuildId, emptied);

        // Act
        await h.ReconcileAsync();

        // Assert
        Assert.Null(h.Discord.Channel(emptiedChannel!.Value));
        Assert.NotNull(h.Discord.Channel(occupiedChannel!.Value));
        var row = Assert.Single(await h.TempChannelsAsync());
        Assert.Equal(occupiedChannel, row.ChannelId);
    }
}
