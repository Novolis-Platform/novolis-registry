using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>Downloads and verifies one release artifact without installing it.</summary>
public interface IArtifactDownloader
{
    /// <summary>Downloads a candidate into host-controlled storage.</summary>
    ValueTask<DownloadedUpdateArtifact> DownloadAsync(
        UpdateReleaseCandidate candidate,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
