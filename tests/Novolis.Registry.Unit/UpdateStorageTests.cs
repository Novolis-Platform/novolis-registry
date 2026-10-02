using System.Net;
using System.Security.Cryptography;
using Novolis.Registry.Primitives.Updates;
using Novolis.Registry.Updates;

namespace Novolis.Registry.Unit;

public sealed class UpdateStorageTests
{
    [Test]
    public async Task File_downloader_verifies_length_hash_and_cleans_partial_files()
    {
        var bytes = "verified update"u8.ToArray();
        var root = Path.Combine(Path.GetTempPath(), $"novolis-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var artifact = UpdateManifestContractTests.CreateArtifact("Example.zip") with
            {
                Length = bytes.Length,
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
                DownloadUri = new Uri("https://downloads.example/Example.zip"),
            };
            var candidate = new UpdateReleaseCandidate
            {
                Manifest = UpdateManifestContractTests.CreateManifest(),
                Artifact = artifact,
            };
            using var client = new HttpClient(new BytesHandler(bytes));
            var downloader = new FileArtifactDownloader(
                client,
                new FileArtifactDownloaderOptions { DestinationDirectory = root });

            var result = await downloader.DownloadAsync(candidate);

            await Assert.That(File.ReadAllBytes(result.Path)).IsEquivalentTo(bytes);
            await Assert.That(result.Length).IsEqualTo(bytes.Length);
            await Assert.That(result.Sha256).IsEqualTo(artifact.Sha256);
            await Assert.That(Directory.GetFiles(root, "*.partial")).IsEmpty();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task File_downloader_rejects_hash_mismatch_and_path_traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"novolis-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var candidate = new UpdateReleaseCandidate
            {
                Manifest = UpdateManifestContractTests.CreateManifest(),
                Artifact = UpdateManifestContractTests.CreateArtifact("../escape.zip") with
                {
                    DownloadUri = new Uri("https://downloads.example/escape.zip"),
                },
            };
            using var client = new HttpClient(new BytesHandler("bad"u8.ToArray()));
            var downloader = new FileArtifactDownloader(
                client,
                new FileArtifactDownloaderOptions { DestinationDirectory = root });

            await Assert.That(async () => await downloader.DownloadAsync(candidate))
                .Throws<UpdateSourceException>();
            await Assert.That(Directory.GetFiles(root)).IsEmpty();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Json_state_store_round_trips_and_replaces_atomically()
    {
        var root = Path.Combine(Path.GetTempPath(), $"novolis-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var store = new JsonUpdateStateStore(Path.Combine(root, "state.json"));
            var state = new UpdatePersistedState
            {
                State = UpdateState.DownloadReady,
                CandidateIdentity = "Novolis.Example|stable|2026.1.2.0|Example.zip",
                Checkpoint = new UpdateSourceCheckpoint
                {
                    ETag = "\"etag\"",
                    LastCheckedAt = UpdateManifestContractTests.Utc("2026-10-03T00:00:00Z"),
                },
            };

            await store.SaveAsync(state);
            var loaded = await store.LoadAsync();

            await Assert.That(loaded).IsEqualTo(state);
            await Assert.That(Directory.GetFiles(root, "*.tmp")).IsEmpty();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class BytesHandler(byte[] bytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes),
            });
    }
}
