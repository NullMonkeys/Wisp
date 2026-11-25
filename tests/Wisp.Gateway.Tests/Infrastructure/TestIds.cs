namespace Wisp.Gateway.Tests.Infrastructure;

public static class TestIds
{
    public static ulong Next() => (ulong)Random.Shared.NextInt64(1, long.MaxValue);
}
