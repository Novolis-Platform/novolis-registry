namespace Novolis.Registry.Primitives;

/// <summary>Versioned catalog document served by a registry source.</summary>
public sealed record RegistryDocument
{
    /// <summary>Schema version of this document.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>UTC time at which the document was generated.</summary>
    public DateTimeOffset? GeneratedAt { get; init; }

    /// <summary>Entries advertised by the document.</summary>
    public IReadOnlyList<RegistryEntry> Entries { get; init; } = [];
}
