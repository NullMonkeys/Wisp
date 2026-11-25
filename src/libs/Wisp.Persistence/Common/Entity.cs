namespace Wisp.Persistence.Common;

public abstract class Entity
{
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    internal void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

public abstract class Entity<TId> : Entity
    where TId : struct, IEquatable<TId>
{
    public TId Id { get; init; }
}
