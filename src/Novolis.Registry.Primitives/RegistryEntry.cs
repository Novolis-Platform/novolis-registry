namespace Novolis.Registry.Primitives;

/// <summary>One package, tool, or application advertised by a registry.</summary>
public sealed record RegistryEntry
{
    /// <summary>Stable registry identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string Name { get; init; }

    /// <summary>Kind of item represented by this entry.</summary>
    public RegistryItemKind Kind { get; init; }

    /// <summary>Published version selected for the entry.</summary>
    public required string Version { get; init; }

    /// <summary>NuGet package identifier when the entry represents a package.</summary>
    public string? PackageId { get; init; }

    /// <summary>Release channel represented by the entry.</summary>
    public RegistryChannel Channel { get; init; } = RegistryChannel.Stable;

    /// <summary>Source repository for the entry, when one exists.</summary>
    public Uri? Repository { get; init; }

    /// <summary>Release notes or release page for the selected version.</summary>
    public Uri? Release { get; init; }

    /// <summary>Downloadable artifacts belonging to the entry.</summary>
    public IReadOnlyList<RegistryArtifact> Artifacts { get; init; } = [];
}
