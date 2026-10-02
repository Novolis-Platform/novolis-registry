using Novolis.Registry.Primitives;

namespace Novolis.Registry.Abstractions;

/// <summary>Provider-neutral registry operations used by clients and future hosts.</summary>
public interface IRegistryService
{
    /// <summary>Gets the current catalog document.</summary>
    ValueTask<RegistryDocument> GetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Finds an entry by its stable identifier.</summary>
    ValueTask<RegistryEntry?> FindAsync(
        string id,
        CancellationToken cancellationToken = default);
}
