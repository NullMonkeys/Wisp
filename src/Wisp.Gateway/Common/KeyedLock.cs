namespace Wisp.Gateway.Common;

/// <summary>
/// An async mutex per key. Entries are reference-counted and removed once nobody holds or waits on them,
/// so locking on unbounded keys (users, guilds) doesn't grow memory forever.
/// </summary>
internal sealed class KeyedLock<TKey> where TKey : notnull
{
    private readonly Dictionary<TKey, Entry> _entries = new();

    public async ValueTask<Releaser> AcquireAsync(TKey key, CancellationToken ct = default)
    {
        Entry entry;
        lock (_entries)
        {
            if (!_entries.TryGetValue(key, out entry!))
                _entries[key] = entry = new Entry();

            entry.RefCount++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(ct);
        }
        catch
        {
            Release(key, entry, acquired: false);
            throw;
        }

        return new Releaser(this, key, entry);
    }

    private void Release(TKey key, Entry entry, bool acquired)
    {
        if (acquired)
            entry.Semaphore.Release();

        lock (_entries)
        {
            if (--entry.RefCount == 0)
                _entries.Remove(key);
        }
    }

    internal sealed class Entry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount;
    }

    internal readonly struct Releaser(KeyedLock<TKey> owner, TKey key, Entry entry) : IDisposable
    {
        public void Dispose() => owner.Release(key, entry, acquired: true);
    }
}
