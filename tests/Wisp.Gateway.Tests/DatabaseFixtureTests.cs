using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Wisp.Gateway.Tests.Infrastructure;
using Wisp.Persistence;

namespace Wisp.Gateway.Tests;

public sealed class DatabaseFixtureTests(DatabaseFixture database)
{
    [Fact]
    public async Task Database_is_running_and_fully_migrated()
    {
        // Arrange
        using var host = database.CreateHost();
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WispDbContext>();

        // Act
        var pending = await dbContext.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(pending);
    }
}
