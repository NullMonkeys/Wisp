namespace Wisp.Persistence.Common;

/// <summary>
/// Base for every stored row. <see cref="UpdatedAt"/> is bumped by <see cref="WispDbContext"/> when a tracked entity is
/// saved as modified; bulk <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> and raw SQL bypass that and must set it themselves.
/// </summary>
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
