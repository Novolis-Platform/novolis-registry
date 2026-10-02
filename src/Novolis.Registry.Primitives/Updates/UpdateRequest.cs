using Novolis.Registry.Primitives;

namespace Novolis.Registry.Primitives.Updates;

/// <summary>Installed-app identity and target used to select one release artifact.</summary>
public sealed record UpdateRequest
{
    /// <summary>Stable application identifier expected in the release manifest.</summary>
    public required string AppId { get; init; }

    /// <summary>Expected direct-distribution repository.</summary>
    public required Uri Repository { get; init; }

    /// <summary>Channel to query.</summary>
    public RegistryChannel Channel { get; init; } = RegistryChannel.Stable;

    /// <summary>Installed distribution authority.</summary>
    public UpdateDistributionMode DistributionMode { get; init; } =
        UpdateDistributionMode.DirectGithub;

    /// <summary>Current installed version.</summary>
    public required string CurrentVersion { get; init; }

    /// <summary>Target operating system.</summary>
    public required UpdatePlatform Platform { get; init; }

    /// <summary>Optional runtime identifier.</summary>
    public string? RuntimeIdentifier { get; init; }

    /// <summary>Optional architecture.</summary>
    public string? Architecture { get; init; }

    /// <summary>Preferred artifact form.</summary>
    public required UpdateArtifactKind ArtifactKind { get; init; }

    /// <summary>Whether prerelease versions may be selected.</summary>
    public bool IncludePrerelease { get; init; }
}
