using Novolis.Registry.Primitives;

namespace Novolis.Registry.Abstractions;

/// <summary>Reads a registry document from a concrete transport or provider.</summary>
public interface IRegistrySource
{
    /// <summary>Loads the current registry document.</summary>
    ValueTask<RegistryDocument> LoadAsync(CancellationToken cancellationToken = default);
}
