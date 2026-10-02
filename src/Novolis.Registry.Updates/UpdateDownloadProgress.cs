namespace Novolis.Registry.Updates;

/// <summary>Clamped progress reported by an artifact downloader.</summary>
public sealed record UpdateDownloadProgress
{
    /// <summary>Bytes received so far.</summary>
    public long BytesReceived { get; init; }

    /// <summary>Expected bytes, when known.</summary>
    public long? TotalBytes { get; init; }

    /// <summary>Normalized progress between zero and one.</summary>
    public double Fraction => TotalBytes is > 0
        ? Math.Clamp((double)BytesReceived / TotalBytes.Value, 0, 1)
        : 0;

    /// <summary>Creates a progress value with safe non-negative inputs.</summary>
    public static UpdateDownloadProgress Create(long bytesReceived, long? totalBytes) =>
        new()
        {
            BytesReceived = Math.Max(0, bytesReceived),
            TotalBytes = totalBytes is > 0 ? totalBytes : null,
        };
}
