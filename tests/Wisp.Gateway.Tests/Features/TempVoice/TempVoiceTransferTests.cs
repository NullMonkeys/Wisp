using NetCord;
using NetCord.Rest;

using Wisp.Gateway.Features.TempVoice;
using Wisp.Gateway.Tests.Infrastructure;

namespace Wisp.Gateway.Tests.Features.TempVoice;

public sealed class TempVoiceTransferTests(DatabaseFixture database)
{
    /// <summary>
    /// Alice gets a temp channel and Bob joins her in it.
    /// </summary>
    private static async Task<(ulong Master, ulong Channel, ulong Alice, ulong Bob)> AliceWithBobAsync(TempVoiceHarness h)
    {
        var master = await h.EnableAndGetMasterAsync();
        var alice = TestIds.Next();
        var bob = TestIds.Next();
        var channel = (await h.JoinAsync(alice, master, displayName: "Alice"))!.Value;
        await h.JoinAsync(bob, channel, displayName: "Bob");
        return (master, channel, alice, bob);
    }

    [Fact]
    public async Task Transfer_hands_the_channel_and_the_owner_rights_to_the_new_owner()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, channel, alice, bob) = await AliceWithBobAsync(h);

        // Act
        var result = await h.TransferAsync(alice, bob);

        // Assert
        Assert.Equal(new TransferResult(TransferStatus.Transferred, channel), result);

        var row = Assert.Single(await h.TempChannelsAsync());
        Assert.Equal(bob, row.OwnerId);

        var after = h.Discord.Channel(channel)!;
        Assert.Null(after.OverwriteFor(alice));
        var owner = after.OverwriteFor(bob);
        Assert.NotNull(owner);
        Assert.True(owner.Value.Allowed.HasFlag(Permissions.ManageChannels));
        Assert.True(owner.Value.Allowed.HasFlag(Permissions.MoveUsers));
        Assert.NotNull(after.OverwriteFor(h.Discord.BotId));
        Assert.Equal("Alice's channel", after.Name);
    }

    [Fact]
    public async Task Transfer_keeps_other_overwrites_and_replaces_the_new_owners_own()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, channel, alice, bob) = await AliceWithBobAsync(h);
        var role = TestIds.Next();
        var existing = h.Discord.Channel(channel)!.Overwrites;
        h.Discord.SetOverwrites(channel,
        [
            .. existing,
            new PermissionOverwriteSnapshot(role, PermissionOverwriteType.Role, default, Permissions.Connect),
            new PermissionOverwriteSnapshot(bob, PermissionOverwriteType.User, default, Permissions.Speak),
        ]);

        // Act
        await h.TransferAsync(alice, bob);

        // Assert
        var after = h.Discord.Channel(channel)!;
        Assert.Equal(Permissions.Connect, after.OverwriteFor(role)!.Value.Denied);
        Assert.Equal(default, after.OverwriteFor(bob)!.Value.Denied);
        Assert.Single(after.Overwrites, o => o.Id == bob);
    }

    [Fact]
    public async Task Transfer_announces_the_new_owner_in_the_channel_and_pings_only_them()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, channel, alice, bob) = await AliceWithBobAsync(h);

        // Act
        await h.TransferAsync(alice, bob);

        // Assert
        var (channelId, message) = Assert.Single(h.Discord.Messages);
        Assert.Equal(channel, channelId);
        Assert.Contains($"<@{bob}>", message.Content);
        Assert.Contains($"<@{alice}>", message.Content);
        Assert.Equal([bob], message.AllowedMentions!.AllowedUsers!);
        Assert.Empty(message.AllowedMentions.AllowedRoles!);
        Assert.False(message.AllowedMentions.Everyone);
    }

    [Fact]
    public async Task A_failed_announcement_does_not_undo_the_transfer()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, _, alice, bob) = await AliceWithBobAsync(h);
        h.Discord.FailMessages = true;

        // Act
        var result = await h.TransferAsync(alice, bob);

        // Assert
        Assert.Equal(TransferStatus.Transferred, result.Status);
        Assert.Equal(bob, Assert.Single(await h.TempChannelsAsync()).OwnerId);
    }

    [Fact]
    public async Task After_a_transfer_the_previous_owner_can_create_a_new_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (master, channel, alice, bob) = await AliceWithBobAsync(h);
        await h.TransferAsync(alice, bob);

        // Act
        var fresh = await h.JoinAsync(alice, master);

        // Assert
        Assert.NotNull(fresh);
        Assert.NotEqual(channel, fresh);
        var rows = await h.TempChannelsAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.ChannelId == channel && r.OwnerId == bob);
        Assert.Contains(rows, r => r.ChannelId == fresh && r.OwnerId == alice);
    }

    [Fact]
    public async Task After_a_transfer_the_new_owner_joining_the_master_channel_is_moved_back_to_it()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (master, channel, alice, bob) = await AliceWithBobAsync(h);
        await h.TransferAsync(alice, bob);

        // Act
        var landed = await h.JoinAsync(bob, master);

        // Assert
        Assert.Equal(channel, landed);
        Assert.Single(await h.TempChannelsAsync());
    }

    [Fact]
    public async Task The_transfers_own_channel_update_saves_no_preferences()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, channel, alice, bob) = await AliceWithBobAsync(h);
        var before = h.Discord.SettingsOf(channel);
        await h.TransferAsync(alice, bob);
        var after = h.Discord.SettingsOf(channel);

        // Act: the CHANNEL_UPDATE Discord sends back for the overwrite swap
        await h.SavePreferencesAsync(channel, before, after);

        // Assert
        Assert.Null(await h.PreferencesAsync(alice));
        Assert.Null(await h.PreferencesAsync(bob));
    }

    [Fact]
    public async Task Changes_after_a_transfer_are_remembered_for_the_new_owner()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, channel, alice, bob) = await AliceWithBobAsync(h);
        await h.TransferAsync(alice, bob);
        var before = h.Discord.SettingsOf(channel);

        // Act
        await h.SavePreferencesAsync(channel, before, before with { Name = "Bob's den" });

        // Assert
        Assert.Equal("Bob's den", (await h.PreferencesAsync(bob))?.Name);
        Assert.Null(await h.PreferencesAsync(alice));
    }

    [Fact]
    public async Task Transfer_is_refused_when_the_caller_owns_no_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, _, _, bob) = await AliceWithBobAsync(h);

        // Act
        var result = await h.TransferAsync(TestIds.Next(), bob);

        // Assert
        Assert.Equal(new TransferResult(TransferStatus.NotOwner), result);
    }

    [Fact]
    public async Task Transfer_to_yourself_is_refused()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, _, alice, _) = await AliceWithBobAsync(h);

        // Act
        var result = await h.TransferAsync(alice, alice);

        // Assert
        Assert.Equal(TransferStatus.TargetIsOwner, result.Status);
        Assert.Equal(alice, Assert.Single(await h.TempChannelsAsync()).OwnerId);
    }

    [Fact]
    public async Task Transfer_is_refused_when_the_target_is_not_in_the_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, channel, alice, bob) = await AliceWithBobAsync(h);
        await h.LeaveAsync(bob);

        // Act
        var result = await h.TransferAsync(alice, bob);

        // Assert
        Assert.Equal(new TransferResult(TransferStatus.TargetNotInChannel, channel), result);
        Assert.Equal(alice, Assert.Single(await h.TempChannelsAsync()).OwnerId);
        Assert.Empty(h.Discord.Messages);
    }

    [Fact]
    public async Task Transfer_is_refused_when_the_target_already_owns_a_channel()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var alice = TestIds.Next();
        var bob = TestIds.Next();
        var alicesChannel = (await h.JoinAsync(alice, master))!.Value;
        var bobsChannel = (await h.JoinAsync(bob, master))!.Value;
        await h.JoinAsync(TestIds.Next(), bobsChannel); // keeps Bob's channel alive once he leaves it
        await h.LeaveAsync(bob);
        await h.JoinAsync(bob, alicesChannel);

        // Act
        var result = await h.TransferAsync(alice, bob);

        // Assert
        Assert.Equal(new TransferResult(TransferStatus.TargetOwnsChannel, alicesChannel), result);
        var rows = await h.TempChannelsAsync();
        Assert.Contains(rows, r => r.ChannelId == alicesChannel && r.OwnerId == alice);
        Assert.Contains(rows, r => r.ChannelId == bobsChannel && r.OwnerId == bob);
    }

    [Fact]
    public async Task A_stale_channel_row_of_the_target_does_not_block_the_transfer()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var master = await h.EnableAndGetMasterAsync();
        var alice = TestIds.Next();
        var bob = TestIds.Next();
        var alicesChannel = (await h.JoinAsync(alice, master))!.Value;
        var bobsChannel = (await h.JoinAsync(bob, master))!.Value;
        h.Discord.DeleteChannel(bobsChannel); // deleted while we weren't looking: the row is left behind
        await h.JoinAsync(bob, alicesChannel);

        // Act
        var result = await h.TransferAsync(alice, bob);

        // Assert
        Assert.Equal(TransferStatus.Transferred, result.Status);
        var row = Assert.Single(await h.TempChannelsAsync());
        Assert.Equal(alicesChannel, row.ChannelId);
        Assert.Equal(bob, row.OwnerId);
    }

    [Fact]
    public async Task When_discord_rejects_the_overwrites_the_owner_is_rolled_back()
    {
        // Arrange
        await using var h = await TempVoiceHarness.CreateAsync(database);
        var (_, channel, alice, bob) = await AliceWithBobAsync(h);
        var overwritesBefore = h.Discord.Channel(channel)!.Overwrites;
        h.Discord.FailOverwriteUpdates = true;

        // Act
        await Assert.ThrowsAsync<RestException>(() => h.TransferAsync(alice, bob));

        // Assert
        Assert.Equal(alice, Assert.Single(await h.TempChannelsAsync()).OwnerId);
        Assert.Equal(overwritesBefore, h.Discord.Channel(channel)!.Overwrites);
        Assert.Empty(h.Discord.Messages);
    }
}
