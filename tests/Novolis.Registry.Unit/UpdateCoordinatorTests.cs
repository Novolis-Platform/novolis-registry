using Novolis.Registry.Primitives;
using Novolis.Registry.Primitives.Updates;
using Novolis.Registry.Updates;

namespace Novolis.Registry.Unit;

public sealed class UpdateCoordinatorTests
{
    [Test]
    public async Task Coordinator_selects_a_new_candidate_and_suppresses_it_after_acknowledgement()
    {
        var source = new FakeSource
        {
            Result = new UpdateSourceResult
            {
                Candidate = Candidate(),
                ETag = "\"one\"",
            },
        };
        var store = new MemoryStateStore();
        await using var coordinator = CreateCoordinator(source, store);

        var available = await coordinator.CheckAsync(force: true);
        await Assert.That(available.State).IsEqualTo(UpdateState.Available);
        await Assert.That(available.ShouldNotify).IsTrue();
        await Assert.That(available.Candidate!.Version).IsEqualTo("2026.1.2.0");

        var acknowledged = await coordinator.AcknowledgeNotificationAsync();
        await Assert.That(acknowledged.ShouldNotify).IsFalse();
        await Assert.That(store.State!.LastNotifiedIdentity).IsEqualTo(available.Candidate.Identity);
    }

    [Test]
    public async Task Coordinator_honors_interval_but_force_check_bypasses_only_the_interval()
    {
        var source = new FakeSource
        {
            Result = new UpdateSourceResult { Candidate = Candidate() },
        };
        await using var coordinator = CreateCoordinator(
            source,
            new MemoryStateStore(),
            new UpdateCoordinatorOptions { CheckInterval = TimeSpan.FromHours(1) });

        await coordinator.CheckAsync();
        await coordinator.CheckAsync();
        await Assert.That(source.Calls).IsEqualTo(1);

        await coordinator.CheckAsync(force: true);
        await Assert.That(source.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task Coordinator_snoozes_and_promotes_a_verified_download()
    {
        var source = new FakeSource
        {
            Result = new UpdateSourceResult { Candidate = Candidate() },
        };
        var downloader = new FakeDownloader();
        var store = new MemoryStateStore();
        await using var coordinator = CreateCoordinator(source, store, downloader: downloader);

        await coordinator.CheckAsync(force: true);
        var deferred = await coordinator.SnoozeAsync(TimeSpan.FromHours(2));
        await Assert.That(deferred.State).IsEqualTo(UpdateState.Deferred);

        var ready = await coordinator.DownloadAsync();
        await Assert.That(ready.State).IsEqualTo(UpdateState.DownloadReady);
        await Assert.That(ready.Download!.Path).IsEqualTo("downloads/example.zip");
        await Assert.That(downloader.Calls).IsEqualTo(1);
        await Assert.That(store.State!.State).IsEqualTo(UpdateState.DownloadReady);
    }

    [Test]
    public async Task Coordinator_rejects_store_managed_builds_before_contacting_provider()
    {
        var source = new FakeSource();
        await using var coordinator = CreateCoordinator(
            source,
            new MemoryStateStore(),
            request: CreateRequest() with
            {
                DistributionMode = UpdateDistributionMode.StoreManaged,
            });

        var snapshot = await coordinator.CheckAsync(force: true);

        await Assert.That(snapshot.State).IsEqualTo(UpdateState.Failed);
        await Assert.That(snapshot.ErrorCode).IsEqualTo("store-managed");
        await Assert.That(source.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task Coordinator_cancellation_does_not_promote_download_or_replace_current_version()
    {
        var source = new FakeSource
        {
            Result = new UpdateSourceResult { Candidate = Candidate() },
        };
        var downloader = new FakeDownloader { Delay = TimeSpan.FromSeconds(2) };
        await using var coordinator = CreateCoordinator(
            source,
            new MemoryStateStore(),
            downloader: downloader);
        await coordinator.CheckAsync(force: true);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        await Assert.That(async () => await coordinator.DownloadAsync(cancellationToken: cancellation.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(coordinator.Snapshot.CurrentVersion).IsEqualTo("2026.1.1.0");
        await Assert.That(coordinator.Snapshot.State).IsEqualTo(UpdateState.Downloading);
    }

    private static UpdateCoordinator CreateCoordinator(
        FakeSource source,
        MemoryStateStore store,
        UpdateCoordinatorOptions? options = null,
        FakeDownloader? downloader = null,
        UpdateRequest? request = null)
    {
        return new UpdateCoordinator(
            request ?? CreateRequest(),
            source,
            store,
            downloader ?? new FakeDownloader(),
            options: options,
            clock: new TestClock());
    }

    private static UpdateRequest CreateRequest() =>
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

    private static UpdateReleaseCandidate Candidate()
    {
        var manifest = UpdateManifestContractTests.CreateManifest();
        return new UpdateReleaseCandidate
        {
            Manifest = manifest,
            Artifact = manifest.Artifacts[0],
        };
    }

    private sealed class FakeSource : IAutoUpdateSource
    {
        public UpdateSourceResult Result { get; init; } = new();
        public int Calls { get; private set; }

        public ValueTask<UpdateSourceResult> CheckAsync(
            UpdateRequest request,
            UpdateSourceCheckpoint checkpoint,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult(Result);
        }
    }

    private sealed class FakeDownloader : IArtifactDownloader
    {
        public TimeSpan Delay { get; init; }
        public int Calls { get; private set; }

        public async ValueTask<DownloadedUpdateArtifact> DownloadAsync(
            UpdateReleaseCandidate candidate,
            IProgress<UpdateDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            progress?.Report(UpdateDownloadProgress.Create(42, candidate.Artifact.Length));
            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, cancellationToken);
            return new DownloadedUpdateArtifact
            {
                CandidateIdentity = candidate.Identity,
                Path = "downloads/example.zip",
                Length = candidate.Artifact.Length,
                Sha256 = candidate.Artifact.Sha256,
            };
        }
    }

    private sealed class MemoryStateStore : IUpdateStateStore
    {
        public UpdatePersistedState? State { get; private set; }

        public ValueTask<UpdatePersistedState?> LoadAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(State);

        public ValueTask SaveAsync(
            UpdatePersistedState state,
            CancellationToken cancellationToken = default)
        {
            State = state;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestClock : IUpdateClock
    {
        public DateTimeOffset UtcNow { get; } =
            DateTimeOffset.Parse("2026-10-03T00:00:00Z").ToUniversalTime();
    }
}
