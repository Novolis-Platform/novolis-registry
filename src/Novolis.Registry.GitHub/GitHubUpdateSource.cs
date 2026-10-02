using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Novolis.IO.GitHub;
using Novolis.Registry.Primitives;
using Novolis.Registry.Primitives.Updates;
using Novolis.Registry.Updates;

namespace Novolis.Registry.GitHub;

/// <summary>
/// Reads GitHub Releases and their standalone <c>Novolis.Update.json</c>
/// assets without coupling the updater to a UI or installer.
/// </summary>
public sealed class GitHubUpdateSource : IAutoUpdateSource
{
    private readonly HttpClient _httpClient;
    private readonly GitHubUpdateSourceOptions _options;

    /// <summary>Creates a source using an injected HTTP client and handler.</summary>
    public GitHubUpdateSource(
        HttpClient httpClient,
        GitHubUpdateSourceOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    /// <inheritdoc />
    public async ValueTask<UpdateSourceResult> CheckAsync(
        UpdateRequest request,
        UpdateSourceCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (request.DistributionMode == UpdateDistributionMode.StoreManaged)
        {
            throw new UpdateSourceException(
                "store-managed",
                "Store-managed installations do not use GitHub Releases updates.");
        }
        if (!RepositoryMatches(request.Repository, _options.Repository))
        {
            throw new UpdateSourceException(
                "repository-mismatch",
                "The update request repository does not match the configured source.");
        }

        using var releaseRequest = CreateRequest(
            new Uri(
                $"https://api.github.com/repos/{Uri.EscapeDataString(_options.Repository.Owner)}/{Uri.EscapeDataString(_options.Repository.Name)}/releases?per_page={_options.MaximumReleases}"),
            checkpoint.ETag);
        using var releaseResponse = await SendAsync(releaseRequest, cancellationToken)
            .ConfigureAwait(false);
        var etag = releaseResponse.Headers.ETag?.Tag ?? checkpoint.ETag;
        if (releaseResponse.StatusCode == HttpStatusCode.NotModified)
        {
            return new UpdateSourceResult
            {
                NotModified = true,
                ETag = etag,
            };
        }

        await EnsureSuccessAsync(releaseResponse, "release-list", cancellationToken)
            .ConfigureAwait(false);
        var releaseJson = await ReadBoundedStringAsync(
                releaseResponse.Content,
                _options.MaximumResponseBytes,
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            using var releases = JsonDocument.Parse(releaseJson);
            if (releases.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new UpdateSourceException(
                    "release-list-invalid",
                    "GitHub returned a release list that was not an array.");
            }

            UpdateReleaseCandidate? best = null;
            foreach (var release in releases.RootElement.EnumerateArray())
            {
                if (!IsEligibleRelease(release, request))
                    continue;

                var manifestAsset = FindAsset(release, _options.ManifestAssetName);
                if (manifestAsset is null)
                    continue;

                var manifest = await ReadManifestAsync(manifestAsset.Value, cancellationToken)
                    .ConfigureAwait(false);
                if (!MatchesRequest(manifest, release, request))
                    continue;

                var artifact = SelectArtifact(manifest, request);
                if (artifact is null)
                    continue;

                var candidate = new UpdateReleaseCandidate
                {
                    Manifest = manifest,
                    Artifact = artifact,
                };
                if (best is null
                    || UpdateVersionComparer.Compare(candidate.Version, best.Version) > 0)
                {
                    best = candidate;
                }
            }

            return new UpdateSourceResult
            {
                Candidate = best,
                ETag = etag,
            };
        }
        catch (JsonException exception)
        {
            throw new UpdateSourceException(
                "release-list-invalid",
                "GitHub returned malformed release metadata.",
                exception);
        }
    }

    private async ValueTask<UpdateManifest> ReadManifestAsync(
        JsonElement asset,
        CancellationToken cancellationToken)
    {
        var browserUrl = asset.GetProperty("browser_download_url").GetString();
        if (!Uri.TryCreate(browserUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new UpdateSourceException(
                "manifest-url-invalid",
                "The release manifest asset did not have an HTTPS URL.");
        }

        using var request = CreateRequest(uri, null);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, "manifest", cancellationToken).ConfigureAwait(false);
        var json = await ReadBoundedStringAsync(
                response.Content,
                _options.MaximumResponseBytes,
                cancellationToken)
            .ConfigureAwait(false);
        try
        {
            return UpdateManifestJson.Deserialize(json);
        }
        catch (UpdateManifestValidationException exception)
        {
            throw new UpdateSourceException(
                "manifest-invalid",
                exception.Message,
                exception);
        }
        catch (JsonException exception)
        {
            throw new UpdateSourceException(
                "manifest-invalid",
                "The release manifest was not valid JSON.",
                exception);
        }
    }

    private HttpRequestMessage CreateRequest(Uri uri, string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        if (!string.IsNullOrWhiteSpace(etag))
            request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        return request;
    }

    private async ValueTask<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw new UpdateSourceException(
                "network-error",
                "The GitHub update source could not be reached.",
                exception);
        }
    }

    private static ValueTask EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return ValueTask.CompletedTask;

        var retryAfter = ParseRetryAfter(response);
        var code = response.StatusCode switch
        {
            HttpStatusCode.NotFound => $"{operation}-not-found",
            HttpStatusCode.Forbidden or (HttpStatusCode)429 => "rate-limited",
            >= HttpStatusCode.InternalServerError => "github-unavailable",
            _ => $"{operation}-http-{(int)response.StatusCode}",
        };
        var message = code == "rate-limited"
            ? "GitHub temporarily rate-limited update checks."
            : $"GitHub returned {(int)response.StatusCode} while loading {operation}.";
        throw new UpdateSourceException(code, message, retryAfter: retryAfter);
    }

    private async ValueTask<string> ReadBoundedStringAsync(
        HttpContent content,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        var length = content.Headers.ContentLength;
        if (length is > 0 && length > maximumBytes)
        {
            throw new UpdateSourceException(
                "response-too-large",
                "The GitHub response exceeded the configured size limit.");
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = new byte[16 * 1024];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maximumBytes)
                throw new UpdateSourceException(
                    "response-too-large",
                    "The GitHub response exceeded the configured size limit.");
            output.Write(buffer, 0, read);
        }

        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private static bool IsEligibleRelease(JsonElement release, UpdateRequest request)
    {
        return release.TryGetProperty("draft", out var draft)
            && !draft.GetBoolean()
            && release.TryGetProperty("prerelease", out var prerelease)
            && (request.IncludePrerelease
                || request.Channel != RegistryChannel.Stable
                || !prerelease.GetBoolean());
    }

    private static JsonElement? FindAsset(JsonElement release, string name)
    {
        if (!release.TryGetProperty("assets", out var assets)
            || assets.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            if (asset.TryGetProperty("name", out var assetName)
                && string.Equals(
                    assetName.GetString(),
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return asset;
            }
        }

        return null;
    }

    private static bool MatchesRequest(
        UpdateManifest manifest,
        JsonElement release,
        UpdateRequest request)
    {
        if (!string.Equals(manifest.AppId, request.AppId, StringComparison.Ordinal))
            return false;
        if (manifest.DistributionMode != UpdateDistributionMode.DirectGithub)
            return false;
        if (manifest.Channel != request.Channel)
            return false;
        if (!RepositoryMatches(request.Repository, manifest.Provenance.Repository))
            return false;
        if (!UpdateVersionComparer.IsValid(manifest.Version))
            return false;
        if (!release.TryGetProperty("html_url", out var releaseUrl)
            || !Uri.TryCreate(releaseUrl.GetString(), UriKind.Absolute, out var releaseUri))
            return false;
        return Uri.Compare(
                   manifest.Provenance.ReleaseUri,
                   releaseUri,
                   UriComponents.AbsoluteUri,
                   UriFormat.Unescaped,
                   StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static UpdateArtifact? SelectArtifact(
        UpdateManifest manifest,
        UpdateRequest request)
    {
        return manifest.Artifacts
            .Where(artifact =>
                artifact.Kind == request.ArtifactKind
                && artifact.Target.Platform == request.Platform
                && (request.RuntimeIdentifier is null
                    || string.Equals(
                        artifact.Target.RuntimeIdentifier,
                        request.RuntimeIdentifier,
                        StringComparison.OrdinalIgnoreCase))
                && (request.Architecture is null
                    || string.Equals(
                        artifact.Target.Architecture,
                        request.Architecture,
                        StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(artifact =>
                string.Equals(
                    artifact.Target.RuntimeIdentifier,
                    request.RuntimeIdentifier,
                    StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(artifact =>
                string.Equals(
                    artifact.Target.Architecture,
                    request.Architecture,
                    StringComparison.OrdinalIgnoreCase))
            .ThenBy(artifact => artifact.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool RepositoryMatches(Uri request, GitHubRepository configured)
    {
        var expected = new Uri($"https://github.com/{configured.Owner}/{configured.Name}");
        return Uri.Compare(
                   request,
                   expected,
                   UriComponents.SchemeAndServer | UriComponents.Path,
                   UriFormat.Unescaped,
                   StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static DateTimeOffset? ParseRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return DateTimeOffset.UtcNow + delta;
        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
            && long.TryParse(values.FirstOrDefault(), out var seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        return null;
    }
}
