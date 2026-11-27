using Wisp.Gateway.Tests.Infrastructure;

namespace Wisp.Gateway.Tests.Features.JoinRoles;

public sealed class JoinRolesMembersTests(DatabaseFixture database)
{
    [Fact]
    public async Task Joining_member_gets_every_join_role()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var member = h.Discord.AddRole();
        var verified = h.Discord.AddRole();
        await h.SetUpAsync(member, verified);
        var userId = TestIds.Next();

        // Act
        var assigned = await h.JoinAsync(userId);

        // Assert
        Assert.Equal(2, assigned);
        Assert.Equal([member, verified], h.Discord.RolesOf(userId));
        Assert.Equal(1, h.Discord.Calls);
    }

    [Fact]
    public async Task Roles_the_member_already_has_are_kept()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var fromOnboarding = h.Discord.AddRole();
        var joinRole = h.Discord.AddRole();
        await h.SetUpAsync(joinRole);
        var userId = TestIds.Next();

        // Act
        await h.JoinAsync(userId, currentRoles: [fromOnboarding]);

        // Assert
        Assert.Equal([fromOnboarding, joinRole], h.Discord.RolesOf(userId));
    }

    [Fact]
    public async Task Member_who_already_has_every_join_role_is_left_alone()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var role = h.Discord.AddRole();
        await h.SetUpAsync(role);

        // Act
        var assigned = await h.JoinAsync(TestIds.Next(), currentRoles: [role]);

        // Assert
        Assert.Equal(0, assigned);
        Assert.Equal(0, h.Discord.Calls);
    }

    [Fact]
    public async Task Joining_member_gets_nothing_while_disabled()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        await h.SetRolesAsync(h.Discord.AddRole());

        // Act
        await h.JoinAsync(TestIds.Next());

        // Assert
        Assert.Equal(0, h.Discord.Calls);
    }

    [Fact]
    public async Task Joining_bot_gets_nothing()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        await h.SetUpAsync(h.Discord.AddRole());

        // Act
        await h.JoinAsync(TestIds.Next(), isBot: true);

        // Assert
        Assert.Equal(0, h.Discord.Calls);
    }

    [Fact]
    public async Task Member_behind_screening_gets_roles_only_once_they_pass_it()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var role = h.Discord.AddRole();
        await h.SetUpAsync(role);
        var userId = TestIds.Next();

        // Act
        await h.JoinAsync(userId, isPending: true);
        var whilePending = h.Discord.RolesOf(userId);
        await h.UpdateAsync(userId, wasPending: true, isPending: false);

        // Assert
        Assert.Empty(whilePending);
        Assert.Equal([role], h.Discord.RolesOf(userId));
    }

    [Fact]
    public async Task Member_update_that_is_not_passing_screening_gives_nothing()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        await h.SetUpAsync(h.Discord.AddRole());

        // Act
        await h.UpdateAsync(TestIds.Next(), wasPending: false, isPending: false);
        await h.UpdateAsync(TestIds.Next(), wasPending: true, isPending: true);

        // Assert
        Assert.Equal(0, h.Discord.Calls);
    }

    [Fact]
    public async Task Role_deleted_before_the_event_arrives_is_skipped()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var kept = h.Discord.AddRole();
        var deleted = h.Discord.AddRole();
        await h.SetUpAsync(kept, deleted);
        h.Discord.DeleteRole(deleted);
        var userId = TestIds.Next();

        // Act
        await h.JoinAsync(userId);

        // Assert
        Assert.Equal([kept], h.Discord.RolesOf(userId));
    }

    [Fact]
    public async Task Failed_update_gives_nothing_and_does_not_throw()
    {
        // Arrange
        await using var h = await JoinRolesHarness.CreateAsync(database);
        var failing = h.Discord.AddRole();
        var fine = h.Discord.AddRole();
        await h.SetUpAsync(failing, fine);
        h.Discord.FailRole(failing);
        var userId = TestIds.Next();

        // Act
        var assigned = await h.JoinAsync(userId);

        // Assert
        Assert.Equal(0, assigned);
        Assert.Empty(h.Discord.RolesOf(userId));
        Assert.Equal(1, h.Discord.Calls);
    }
}
