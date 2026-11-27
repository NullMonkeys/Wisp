using Wisp.Gateway.Features.JoinRoles;
using Wisp.Gateway.Tests.Infrastructure;
using Wisp.Persistence.Entities;

namespace Wisp.Gateway.Tests.Features.JoinRoles;

public sealed class JoinRolesSettingsTests(DatabaseFixture database)
{
    [Fact]
    public async Task Set_roles_saves_the_selected_roles()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var member = h.Discord.AddRole();
        var verified = h.Discord.AddRole();

        // Act
        var result = await h.SetRolesAsync(member, verified);

        // Assert
        Assert.Empty(result.Rejected);
        Assert.Equal([member, verified], (await h.GetConfigAsync()).RoleIds);
    }

    [Fact]
    public async Task Set_roles_rejects_roles_the_bot_cannot_assign_and_keeps_the_rest()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        h.Discord.Bot = new BotSnapshot(TopRolePosition: 10, CanManageRoles: true);
        var fine = h.Discord.AddRole(position: 5);
        var managed = h.Discord.AddRole(position: 5, managed: true);
        var tied = h.Discord.AddRole(position: 10);
        var above = h.Discord.AddRole(position: 20);
        var unknown = TestIds.Next();

        // Act
        var result = await h.SetRolesAsync(fine, h.GuildId, managed, tied, above, unknown);

        // Assert
        Assert.Equal([fine], (await h.GetConfigAsync()).RoleIds);
        Assert.Equal(
            [
                new RejectedRole(h.GuildId, RoleRejection.Everyone),
                new RejectedRole(managed, RoleRejection.Managed),
                new RejectedRole(tied, RoleRejection.AboveBot),
                new RejectedRole(above, RoleRejection.AboveBot),
                new RejectedRole(unknown, RoleRejection.Unknown),
            ],
            result.Rejected);
    }

    [Fact]
    public async Task Set_roles_when_the_bot_is_not_cached_rejects_everything()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        h.Discord.Bot = null;
        var role = h.Discord.AddRole();

        // Act
        var result = await h.SetRolesAsync(role);

        // Assert
        Assert.Empty((await h.GetConfigAsync()).RoleIds);
        Assert.Equal([new RejectedRole(role, RoleRejection.AboveBot)], result.Rejected);
    }

    [Fact]
    public async Task Set_roles_keeps_the_first_ten_and_rejects_the_rest()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var roles = Enumerable.Range(0, JoinRolesConfig.MaxRoles + 2).Select(_ => h.Discord.AddRole()).ToArray();

        // Act
        var result = await h.SetRolesAsync(roles);

        // Assert
        Assert.Equal(roles[..JoinRolesConfig.MaxRoles], (await h.GetConfigAsync()).RoleIds);
        Assert.Equal(roles[JoinRolesConfig.MaxRoles..], result.Rejected.Select(r => r.RoleId));
        Assert.All(result.Rejected, r => Assert.Equal(RoleRejection.OverLimit, r.Reason));
    }

    [Fact]
    public async Task Set_roles_with_nothing_selected_clears_the_roles()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        await h.SetUpAsync(h.Discord.AddRole());

        // Act
        await h.SetRolesAsync();

        // Assert
        var config = await h.GetConfigAsync();
        Assert.Empty(config.RoleIds);
        Assert.True(config.Enabled);
    }

    [Fact]
    public async Task Enable_without_roles_does_nothing()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);

        // Act
        var config = await h.EnableAsync();

        // Assert
        Assert.False(config.Enabled);
        Assert.False((await h.GetConfigAsync()).Enabled);
    }

    [Fact]
    public async Task Enable_with_roles_turns_it_on()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        await h.SetRolesAsync(h.Discord.AddRole());

        // Act
        var config = await h.EnableAsync();

        // Assert
        Assert.True(config.Enabled);
        Assert.True((await h.GetConfigAsync()).Enabled);
    }

    [Fact]
    public async Task Disable_keeps_the_roles()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var role = h.Discord.AddRole();
        await h.SetUpAsync(role);

        // Act
        await h.DisableAsync();

        // Assert
        var config = await h.GetConfigAsync();
        Assert.False(config.Enabled);
        Assert.Equal([role], config.RoleIds);
    }

    [Fact]
    public async Task Deleting_a_role_removes_it_from_the_config()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var kept = h.Discord.AddRole();
        var deleted = h.Discord.AddRole();
        await h.SetUpAsync(kept, deleted);

        // Act
        await h.DeleteRoleAsync(deleted);

        // Assert
        Assert.Equal([kept], (await h.GetConfigAsync()).RoleIds);
    }

    [Fact]
    public async Task Deleting_the_guild_removes_its_config()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        await h.SetUpAsync(h.Discord.AddRole());

        // Act
        await h.DeleteGuildAsync();

        // Assert
        var config = await h.GetConfigAsync();
        Assert.False(config.Enabled);
        Assert.Empty(config.RoleIds);
    }
}
