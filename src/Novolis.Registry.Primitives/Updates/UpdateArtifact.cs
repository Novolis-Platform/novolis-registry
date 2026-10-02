namespace Novolis.Registry.Primitives.Updates;

/// <summary>Verified release asset selected for an installed target.</summary>
public sealed record UpdateArtifact
{
    /// <summary>Exact asset basename published by the release.</summary>
    public required string Name { get; init; }

    /// <summary>Installation or handoff form of the asset.</summary>
    public required UpdateArtifactKind Kind { get; init; }

    /// <summary>Operating-system target of the asset.</summary>
    public required UpdateTarget Target { get; init; }

    /// <summary>HTTPS download URL for the asset.</summary>
    public required Uri DownloadUri { get; init; }

    /// <summary>Expected byte length. A non-positive value means unknown.</summary>
    public long Length { get; init; }

    /// <summary>Lowercase hexadecimal SHA-256 digest.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Optional media type supplied by the release pipeline.</summary>
    public string? ContentType { get; init; }
}
