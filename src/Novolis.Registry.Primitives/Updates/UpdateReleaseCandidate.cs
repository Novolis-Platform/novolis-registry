namespace Novolis.Registry.Primitives.Updates;

/// <summary>A manifest and artifact that are eligible for the installed target.</summary>
public sealed record UpdateReleaseCandidate
{
    /// <summary>Validated release manifest.</summary>
    public required UpdateManifest Manifest { get; init; }

    /// <summary>Selected target artifact.</summary>
    public required UpdateArtifact Artifact { get; init; }

    /// <summary>Convenience access to the selected version.</summary>
    public string Version => Manifest.Version;

    /// <summary>Convenience access to the release notes.</summary>
    public string? ReleaseNotes => Manifest.ReleaseNotes;

    /// <summary>Convenience access to the release page.</summary>
    public Uri ReleaseUri => Manifest.Provenance.ReleaseUri;

    /// <summary>Stable notification identity.</summary>
    public string Identity =>
        $"{Manifest.AppId}|{Manifest.Channel}|{Manifest.Version}|{Artifact.Name}";
}
