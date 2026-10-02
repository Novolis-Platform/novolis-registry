using System.Security.Cryptography;
using System.Collections.Concurrent;
using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>
/// Streams a release asset to a temporary file, verifies length and SHA-256,
/// then atomically promotes it to the host download directory.
/// </summary>
public sealed class FileArtifactDownloader : IArtifactDownloader
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> DownloadGates = new(
        StringComparer.OrdinalIgnoreCase);
    private readonly HttpClient _httpClient;
    private readonly FileArtifactDownloaderOptions _options;

    /// <summary>Creates a downloader using an injected HTTP client.</summary>
    public FileArtifactDownloader(
        HttpClient httpClient,
        FileArtifactDownloaderOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    /// <inheritdoc />
    public async ValueTask<DownloadedUpdateArtifact> DownloadAsync(
        UpdateReleaseCandidate candidate,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ValidateArtifactName(candidate.Artifact.Name);
        if (candidate.Artifact.DownloadUri.Scheme != Uri.UriSchemeHttps)
            throw new UpdateSourceException("insecure-download", "Update downloads must use HTTPS.");

        Directory.CreateDirectory(_options.DestinationDirectory);
        var destination = Path.Combine(
            Path.GetFullPath(_options.DestinationDirectory),
            candidate.Artifact.Name);
        var gate = DownloadGates.GetOrAdd(
            destination,
            static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await TryUseExistingAsync(candidate, destination, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return existing;

            return await DownloadCoreAsync(
                    candidate,
                    destination,
                    progress,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async ValueTask<DownloadedUpdateArtifact?> TryUseExistingAsync(
        UpdateReleaseCandidate candidate,
        string destination,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(destination))
            return null;
        var info = new FileInfo(destination);
        if (candidate.Artifact.Length > 0 && info.Length != candidate.Artifact.Length)
            return null;
        await using var stream = new FileStream(
            destination,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = Convert.ToHexString(
                await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false))
            .ToLowerInvariant();
        if (!string.Equals(hash, candidate.Artifact.Sha256, StringComparison.OrdinalIgnoreCase))
            return null;
        return new DownloadedUpdateArtifact
        {
            CandidateIdentity = candidate.Identity,
            Path = destination,
            Length = info.Length,
            Sha256 = hash,
        };
    }

    private async ValueTask<DownloadedUpdateArtifact> DownloadCoreAsync(
        UpdateReleaseCandidate candidate,
        string destination,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var temporary = $"{destination}.{Guid.NewGuid():N}.partial";

        try
        {
            using var response = await _httpClient.GetAsync(
                    candidate.Artifact.DownloadUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new UpdateSourceException(
                    $"http-{(int)response.StatusCode}",
                    $"The update artifact request returned {(int)response.StatusCode}.");

            var responseLength = response.Content.Headers.ContentLength;
            if (responseLength is > 0
                && responseLength > _options.MaximumBytes)
            {
                throw new UpdateSourceException(
                    "response-too-large",
                    "The update artifact exceeds the configured size limit.");
            }

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            long received = 0;
            string digest;
            await using (var output = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    received += read;
                    if (received > _options.MaximumBytes
                        || (candidate.Artifact.Length > 0 && received > candidate.Artifact.Length))
                    {
                        throw new UpdateSourceException(
                            "response-too-large",
                            "The update artifact exceeded its declared size.");
                    }

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                        .ConfigureAwait(false);
                    hash.AppendData(buffer, 0, read);
                    progress?.Report(UpdateDownloadProgress.Create(
                        received,
                        candidate.Artifact.Length > 0
                            ? candidate.Artifact.Length
                            : responseLength));
                }

                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
                output.Flush(flushToDisk: true);
                digest = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            }

            if (candidate.Artifact.Length > 0 && received != candidate.Artifact.Length)
                throw new UpdateSourceException(
                    "length-mismatch",
                    "The downloaded update length did not match its manifest.");
            if (!string.Equals(digest, candidate.Artifact.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new UpdateSourceException(
                    "hash-mismatch",
                    "The downloaded update checksum did not match its manifest.");

            File.Move(temporary, destination, overwrite: true);
            return new DownloadedUpdateArtifact
            {
                CandidateIdentity = candidate.Identity,
                Path = destination,
                Length = received,
                Sha256 = digest,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static void ValidateArtifactName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || name is "." or ".."
            || name.Contains('/', StringComparison.Ordinal)
            || name.Contains('\\', StringComparison.Ordinal)
            || name.Contains("..", StringComparison.Ordinal))
        {
            throw new UpdateSourceException(
                "unsafe-artifact-name",
                "The update artifact name is not a safe file basename.");
        }
    }
}
