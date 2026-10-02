using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Novolis.IO.GitHub;
using Novolis.Registry.GitHub;
using Novolis.Registry.Primitives.Updates;
using Novolis.Registry.Updates;

namespace Novolis.Registry.Unit;

public sealed class UpdateIntegrationTests
{
    [Test]
    public async Task Discovery_download_state_and_platform_handoff_promote_one_verified_artifact()
    {
        var bytes = "new release bytes"u8.ToArray();
        var root = Path.Combine(Path.GetTempPath(), $"novolis-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var manifest = Manifest(bytes);
            var handler = new ReleaseHandler(manifest, bytes);
            using var client = new HttpClient(handler);
            var source = new GitHubUpdateSource(
                client,
                new GitHubUpdateSourceOptions
                {
                    Repository = new GitHubRepository
                    {
                        Owner = "Novolis-Platform",
                        Name = "example",
                    },
                });
            var action = new RecordingAction();
            await using var coordinator = new UpdateCoordinator(
                Request(),
                source,
                new JsonUpdateStateStore(Path.Combine(root, "state.json")),
                new FileArtifactDownloader(
                    client,
                    new FileArtifactDownloaderOptions { DestinationDirectory = root }),
                action);

            var available = await coordinator.CheckAsync(force: true);
            var ready = await coordinator.DownloadAsync();
            await coordinator.ApplyAsync();

            await Assert.That(available.State).IsEqualTo(UpdateState.Available);
            await Assert.That(ready.State).IsEqualTo(UpdateState.DownloadReady);
            await Assert.That(action.Applied).Count().IsEqualTo(1);
            await Assert.That(File.ReadAllBytes(action.Applied[0])).IsEquivalentTo(bytes);
            await Assert.That(handler.ArtifactRequests).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Concurrent_downloads_share_verified_file_and_checksum_failure_keeps_existing_file()
    {
        var bytes = "new release bytes"u8.ToArray();
        var root = Path.Combine(Path.GetTempPath(), $"novolis-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var manifest = Manifest(bytes);
            var handler = new ReleaseHandler(manifest, bytes);
            using var client = new HttpClient(handler);
            var downloader = new FileArtifactDownloader(
                client,
                new FileArtifactDownloaderOptions { DestinationDirectory = root });
            var candidate = new UpdateReleaseCandidate
            {
                Manifest = manifest,
                Artifact = manifest.Artifacts[0],
            };

            var downloads = await Task.WhenAll(
                downloader.DownloadAsync(candidate).AsTask(),
                downloader.DownloadAsync(candidate).AsTask());

            await Assert.That(downloads[0].Path).IsEqualTo(downloads[1].Path);
            await Assert.That(handler.ArtifactRequests).IsEqualTo(1);

            var old = "old release bytes"u8.ToArray();
            var mismatchHandler = new ReleaseHandler(manifest, old);
            using var mismatchClient = new HttpClient(mismatchHandler);
            var mismatchDownloader = new FileArtifactDownloader(
                mismatchClient,
                new FileArtifactDownloaderOptions { DestinationDirectory = root });
            await File.WriteAllBytesAsync(downloads[0].Path, old);

            await Assert.That(async () => await mismatchDownloader.DownloadAsync(candidate))
                .Throws<UpdateSourceException>();
            await Assert.That(File.ReadAllBytes(downloads[0].Path)).IsEquivalentTo(old);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static UpdateManifest Manifest(byte[] bytes)
    {
        var version = "2026.1.2.0";
        var name = "Example.zip";
        return UpdateManifestContractTests.CreateManifest() with
        {
            Version = version,
            Provenance = new UpdateProvenance
            {
                AppId = "Novolis.Example",
                Repository = new Uri("https://github.com/Novolis-Platform/example"),
                Tag = $"v{version}",
                ReleaseUri = new Uri(
                    "https://github.com/Novolis-Platform/example/releases/tag/v2026.1.2.0"),
            },
            Artifacts =
            [
                UpdateManifestContractTests.CreateArtifact(name) with
                {
                    Length = bytes.Length,
                    Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                    DownloadUri = new Uri(
                        "https://github.com/Novolis-Platform/example/releases/download/v2026.1.2.0/Example.zip"),
                },
            ],
        };
    }

    private static UpdateRequest Request() =>
        new()
        {
            AppId = "Novolis.Example",
            Repository = new Uri("https://github.com/Novolis-Platform/example"),
            CurrentVersion = "2026.1.1.0",
            Platform = UpdatePlatform.Windows,
            RuntimeIdentifier = "win-x64",
            Architecture = "x64",
            ArtifactKind = UpdateArtifactKind.WindowsPortable,
        };

    private sealed class RecordingAction : IPlatformUpdateAction
    {
        public List<string> Applied { get; } = [];
        public bool CanApply => true;

        public ValueTask ApplyAsync(
            DownloadedUpdateArtifact artifact,
            CancellationToken cancellationToken = default)
        {
            Applied.Add(artifact.Path);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ReleaseHandler(
        UpdateManifest manifest,
        byte[] artifactBytes) : HttpMessageHandler
    {
        private int _artifactRequests;

        public int ArtifactRequests => Volatile.Read(ref _artifactRequests);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/releases", StringComparison.Ordinal))
            {
                var version = manifest.Version;
                var json = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        draft = false,
                        prerelease = false,
                        html_url = manifest.Provenance.ReleaseUri.ToString(),
                        assets = new[]
                        {
                            new
                            {
                                name = "Novolis.Update.json",
                                browser_download_url =
                                    $"https://github.com/Novolis-Platform/example/releases/download/v{version}/Novolis.Update.json",
                            },
                        },
                    },
                });
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"e2e\"");
                return Task.FromResult(response);
            }

            if (path.EndsWith("Novolis.Update.json", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        UpdateManifestJson.SerializeCatalog(new UpdateManifestCatalog
                        {
                            GeneratedAt = manifest.PublishedAt,
                            Updates = [manifest],
                        }),
                        Encoding.UTF8,
                        "application/json"),
                });
            }

            Interlocked.Increment(ref _artifactRequests);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(artifactBytes),
            });
        }
    }
}
