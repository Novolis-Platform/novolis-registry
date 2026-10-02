namespace Novolis.Registry.Updates;

/// <summary>Verified local representation of a downloaded update artifact.</summary>
public sealed record DownloadedUpdateArtifact
{
    /// <summary>Candidate identity that was downloaded.</summary>
    public required string CandidateIdentity { get; init; }

    /// <summary>Absolute path to the verified file.</summary>
    public required string Path { get; init; }

    /// <summary>Number of bytes written.</summary>
    public required long Length { get; init; }

    /// <summary>SHA-256 digest of the verified file.</summary>
    public required string Sha256 { get; init; }
}
