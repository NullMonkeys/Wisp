namespace Wisp.Gateway.Tests.Infrastructure;

/// <summary>
/// Random snowflake-like IDs. All tests share one database, so random IDs keep their rows from colliding.
/// </summary>
public static class TestIds
{
    public static ulong Next() => (ulong)Random.Shared.NextInt64(1, long.MaxValue);
}
