namespace Novolis.Registry.Primitives.Updates;

/// <summary>
/// Versioned metadata published as <c>Novolis.Update.json</c> beside a release.
/// </summary>
public sealed record UpdateManifest
{
    /// <summary>Schema version of this standalone document.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Stable application identifier.</summary>
    public required string AppId { get; init; }

    /// <summary>Human-readable application name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Authority that owns installation for this build.</summary>
    public UpdateDistributionMode DistributionMode { get; init; } =
        UpdateDistributionMode.DirectGithub;

    /// <summary>Release channel represented by this manifest.</summary>
    public RegistryChannel Channel { get; init; } = RegistryChannel.Stable;

    /// <summary>Comparable application version.</summary>
    public required string Version { get; init; }

    /// <summary>Release publication time, in UTC.</summary>
    public DateTimeOffset PublishedAt { get; init; }

    /// <summary>Optional minimum installed version for this update.</summary>
    public string? MinimumVersion { get; init; }

    /// <summary>Whether the host should present this update as required.</summary>
    public bool Mandatory { get; init; }

    /// <summary>Release notes rendered by host UI.</summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>Release identity and repository provenance.</summary>
    public required UpdateProvenance Provenance { get; init; }

    /// <summary>All artifacts published for the release.</summary>
    public IReadOnlyList<UpdateArtifact> Artifacts { get; init; } = [];
}
