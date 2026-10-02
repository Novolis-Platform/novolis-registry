namespace Novolis.Registry.Updates;

/// <summary>Atomic persistence boundary for update coordination state.</summary>
public interface IUpdateStateStore
{
    /// <summary>Loads the last committed state, or null for a first run.</summary>
    ValueTask<UpdatePersistedState?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Atomically commits the supplied state.</summary>
    ValueTask SaveAsync(
        UpdatePersistedState state,
        CancellationToken cancellationToken = default);
}
