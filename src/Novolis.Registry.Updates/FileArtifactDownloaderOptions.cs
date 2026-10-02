namespace Novolis.Registry.Updates;

/// <summary>Filesystem and transport limits for verified update downloads.</summary>
public sealed record FileArtifactDownloaderOptions
{
    /// <summary>Directory in which verified and temporary files are stored.</summary>
    public required string DestinationDirectory { get; init; }

    /// <summary>Maximum response size accepted from the network.</summary>
    public long MaximumBytes { get; init; } = 2L * 1024 * 1024 * 1024;

    /// <summary>Validates the options.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DestinationDirectory))
            throw new ArgumentException("A destination directory is required.", nameof(DestinationDirectory));
        if (MaximumBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumBytes));
    }
}
