namespace Novolis.Registry.Primitives.Updates;

/// <summary>Release-level catalog containing one update manifest per application.</summary>
public sealed record UpdateManifestCatalog
{
    /// <summary>Schema version shared by the standalone catalog asset.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>UTC time at which the catalog was generated.</summary>
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>Application update manifests published by this release.</summary>
    public IReadOnlyList<UpdateManifest> Updates { get; init; } = [];
}
