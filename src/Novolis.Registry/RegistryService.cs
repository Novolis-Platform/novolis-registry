using Novolis.Registry.Abstractions;
using Novolis.Registry.Primitives;

namespace Novolis.Registry;

/// <summary>
/// Provider-neutral registry service that loads a catalog and resolves entries.
/// </summary>
public sealed class RegistryService : IRegistryService
{
    private readonly IRegistrySource _source;

    /// <summary>Creates a registry service over the supplied source.</summary>
    public RegistryService(IRegistrySource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <inheritdoc />
    public ValueTask<RegistryDocument> GetCatalogAsync(
        CancellationToken cancellationToken = default) =>
        _source.LoadAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<RegistryEntry?> FindAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var document = await GetCatalogAsync(cancellationToken).ConfigureAwait(false);
        return document.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
