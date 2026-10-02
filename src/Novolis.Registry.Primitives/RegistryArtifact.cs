namespace Novolis.Registry.Primitives;

/// <summary>One downloadable artifact advertised by a registry entry.</summary>
public sealed record RegistryArtifact
{
    /// <summary>Stable artifact name, such as an installer or APK filename.</summary>
    public required string Name { get; init; }

    /// <summary>Target platform for the artifact.</summary>
    public RegistryPlatform Platform { get; init; } = RegistryPlatform.Any;

    /// <summary>Optional architecture identifier, such as <c>x64</c> or <c>arm64</c>.</summary>
    public string? Architecture { get; init; }

    /// <summary>Artifact kind, such as <c>installer</c>, <c>apk</c>, or <c>tar.gz</c>.</summary>
    public required string Kind { get; init; }

    /// <summary>HTTPS location from which the artifact can be downloaded.</summary>
    public required Uri DownloadUri { get; init; }

    /// <summary>Optional lowercase SHA-256 digest of the artifact.</summary>
    public string? Sha256 { get; init; }
}
