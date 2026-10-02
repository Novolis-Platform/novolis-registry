using System.Security.Cryptography;
using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>
/// Streams a release asset to a temporary file, verifies length and SHA-256,
/// then atomically promotes it to the host download directory.
/// </summary>
public sealed class FileArtifactDownloader : IArtifactDownloader
{
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
            await using var output = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            var buffer = new byte[64 * 1024];
            long received = 0;
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
            var digest = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
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
