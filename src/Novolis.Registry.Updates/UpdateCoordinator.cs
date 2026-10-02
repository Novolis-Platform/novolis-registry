using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>
/// Coordinates conditional checks, notification suppression, verified downloads,
/// and host-provided platform handoff without taking a dependency on a UI or OS.
/// </summary>
public sealed class UpdateCoordinator : IAsyncDisposable
{
    private readonly UpdateRequest _request;
    private readonly IAutoUpdateSource _source;
    private readonly IUpdateStateStore _stateStore;
    private readonly IArtifactDownloader _downloader;
    private readonly IPlatformUpdateAction? _platformAction;
    private readonly UpdateCoordinatorOptions _options;
    private readonly IUpdateClock _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UpdatePersistedState _persisted = new();
    private UpdateReleaseCandidate? _candidate;
    private DownloadedUpdateArtifact? _download;
    private UpdateSnapshot _snapshot;
    private bool _loaded;
    private bool _disposed;

    /// <summary>Creates a coordinator for one installed app and target.</summary>
    public UpdateCoordinator(
        UpdateRequest request,
        IAutoUpdateSource source,
        IUpdateStateStore stateStore,
        IArtifactDownloader downloader,
        IPlatformUpdateAction? platformAction = null,
        UpdateCoordinatorOptions? options = null,
        IUpdateClock? clock = null)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _platformAction = platformAction;
        _options = options ?? new UpdateCoordinatorOptions();
        _options.Validate();
        _clock = clock ?? new SystemUpdateClock();
        _snapshot = new UpdateSnapshot { CurrentVersion = request.CurrentVersion };
    }

    /// <summary>Raised after a state projection changes.</summary>
    public event Action<UpdateSnapshot>? SnapshotChanged;

    /// <summary>Current immutable state projection.</summary>
    public UpdateSnapshot Snapshot => _snapshot;

    /// <summary>Loads persisted state without contacting the provider.</summary>
    public async ValueTask<UpdateSnapshot> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return _snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Checks for an eligible update when the interval/backoff gate allows it.</summary>
    public ValueTask<UpdateSnapshot> CheckAsync(
        bool force = false,
        CancellationToken cancellationToken = default) =>
        CheckCoreAsync(force, cancellationToken);

    /// <summary>Marks the current candidate notification as seen.</summary>
    public async ValueTask<UpdateSnapshot> AcknowledgeNotificationAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (_candidate is not null
                && !string.Equals(
                    _persisted.LastNotifiedIdentity,
                    _candidate.Identity,
                    StringComparison.Ordinal))
            {
                _persisted = _persisted with { LastNotifiedIdentity = _candidate.Identity };
                await PersistAsync(cancellationToken).ConfigureAwait(false);
                Publish(_snapshot with { ShouldNotify = false });
            }

            return _snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Defers the current candidate for the configured snooze duration.</summary>
    public async ValueTask<UpdateSnapshot> SnoozeAsync(
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (_candidate is null)
                return _snapshot;

            var until = _clock.UtcNow + (duration ?? _options.SnoozeDuration);
            if (until <= _clock.UtcNow)
                throw new ArgumentOutOfRangeException(nameof(duration));

            _persisted = _persisted with { SnoozedUntil = until };
            await PersistAsync(cancellationToken).ConfigureAwait(false);
            Publish(_snapshot with
            {
                State = UpdateState.Deferred,
                ShouldNotify = false,
            });
            return _snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Downloads and verifies the current candidate into host storage.</summary>
    public async ValueTask<UpdateSnapshot> DownloadAsync(
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (_candidate is null)
                return Fail("no-candidate", "There is no update candidate to download.");

            Publish(_snapshot with
            {
                State = UpdateState.Downloading,
                Progress = UpdateDownloadProgress.Create(0, _candidate.Artifact.Length),
                ErrorCode = null,
                ErrorMessage = null,
            });

            var observedProgress = new Progress<UpdateDownloadProgress>(value =>
            {
                var safe = UpdateDownloadProgress.Create(value.BytesReceived, value.TotalBytes);
                progress?.Report(safe);
                Publish(_snapshot with { Progress = safe });
            });

            try
            {
                var downloaded = await _downloader.DownloadAsync(
                        _candidate,
                        observedProgress,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!string.Equals(
                        downloaded.CandidateIdentity,
                        _candidate.Identity,
                        StringComparison.Ordinal))
                {
                    throw new UpdateSourceException(
                        "download-identity-mismatch",
                        "The downloaded artifact did not match the selected release.");
                }

                _download = downloaded;
                _persisted = _persisted with
                {
                    State = UpdateState.DownloadReady,
                    DownloadPath = downloaded.Path,
                    Download = downloaded,
                    ErrorCode = null,
                    ErrorMessage = null,
                };
                await PersistAsync(cancellationToken).ConfigureAwait(false);
                Publish(_snapshot with
                {
                    State = UpdateState.DownloadReady,
                    Progress = UpdateDownloadProgress.Create(
                        downloaded.Length,
                        _candidate.Artifact.Length),
                    Download = downloaded,
                    ErrorCode = null,
                    ErrorMessage = null,
                });
                return _snapshot;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return await FailAndPersistAsync(
                        "download-failed",
                        exception.Message,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Hands a verified download to the configured installer/package installer.</summary>
    public async ValueTask ApplyAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (_download is null)
                throw new InvalidOperationException("No verified update is ready.");
            if (_platformAction is not { CanApply: true })
                throw new InvalidOperationException("This host has no update handoff.");

            await _platformAction.ApplyAsync(_download, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _gate.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask<UpdateSnapshot> CheckCoreAsync(
        bool force,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            var now = _clock.UtcNow;
            if (!force && !CanCheck(now))
                return _snapshot;
            if (force && _persisted.Checkpoint.RetryAfter is { } retryAfter && retryAfter > now)
                return _snapshot;

            if (_request.DistributionMode == UpdateDistributionMode.StoreManaged)
                return await FailAndPersistAsync(
                        "store-managed",
                        "Store-managed installations do not use the direct GitHub updater.",
                        cancellationToken)
                    .ConfigureAwait(false);

            Publish(_snapshot with
            {
                State = UpdateState.Checking,
                ErrorCode = null,
                ErrorMessage = null,
                Progress = null,
            });

            try
            {
                var result = await _source.CheckAsync(
                        _request,
                        _persisted.Checkpoint,
                        cancellationToken)
                    .ConfigureAwait(false);
                var retry = result.RetryAfter is { } providerRetry
                    ? now + Min(providerRetry - now, _options.MaximumRetryDelay)
                    : null;
                var checkpoint = _persisted.Checkpoint with
                {
                    ETag = result.ETag ?? _persisted.Checkpoint.ETag,
                    LastCheckedAt = now,
                    RetryAfter = retry,
                };
                _persisted = _persisted with { Checkpoint = checkpoint };

                if (result.Candidate is not null)
                {
                    var comparison = UpdateVersionComparer.Compare(
                        result.Candidate.Version,
                        _request.CurrentVersion);
                    if (comparison > 0)
                    {
                        _candidate = result.Candidate;
                    }
                    else
                    {
                        _candidate = null;
                    }
                }
                else if (!result.NotModified)
                {
                    _candidate = null;
                }

                var isDeferred = _candidate is not null
                    && _persisted.SnoozedUntil is { } snoozedUntil
                    && snoozedUntil > now;
                var state = _candidate is null
                    ? UpdateState.UpToDate
                    : isDeferred
                        ? UpdateState.Deferred
                        : UpdateState.Available;
                var shouldNotify = _candidate is not null
                    && !isDeferred
                    && !string.Equals(
                        _persisted.LastNotifiedIdentity,
                        _candidate.Identity,
                        StringComparison.Ordinal);

                _persisted = _persisted with
                {
                    State = state,
                    CandidateIdentity = _candidate?.Identity,
                    Candidate = _candidate,
                    SnoozedUntil = isDeferred ? _persisted.SnoozedUntil : null,
                    ErrorCode = null,
                    ErrorMessage = null,
                };
                await PersistAsync(cancellationToken).ConfigureAwait(false);
                Publish(_snapshot with
                {
                    State = state,
                    Candidate = _candidate,
                    ShouldNotify = shouldNotify,
                    ErrorCode = null,
                    ErrorMessage = null,
                });
                return _snapshot;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UpdateSourceException exception)
            {
                return await FailAndPersistAsync(
                        exception.Code,
                        exception.Message,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return await FailAndPersistAsync(
                        "check-failed",
                        exception.Message,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool CanCheck(DateTimeOffset now)
    {
        if (_persisted.Checkpoint.RetryAfter is { } retryAfter && retryAfter > now)
            return false;
        return _persisted.Checkpoint.LastCheckedAt is not { } lastChecked
            || lastChecked + _options.CheckInterval <= now;
    }

    private async ValueTask EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
            return;

        _persisted = await _stateStore.LoadAsync(cancellationToken)
            .ConfigureAwait(false) ?? new UpdatePersistedState();
        _candidate = _persisted.Candidate;
        _download = _persisted.Download;
        if (_download is null && !string.IsNullOrWhiteSpace(_persisted.DownloadPath))
        {
            _download = null;
        }

        _snapshot = _snapshot with
        {
            State = _persisted.State,
            Candidate = _candidate,
            Download = _download,
            ShouldNotify = _candidate is not null
                && !string.Equals(
                    _persisted.LastNotifiedIdentity,
                    _candidate.Identity,
                    StringComparison.Ordinal),
            ErrorCode = _persisted.ErrorCode,
            ErrorMessage = _persisted.ErrorMessage,
        };
        _loaded = true;
    }

    private async ValueTask<UpdateSnapshot> FailAndPersistAsync(
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        var failed = Fail(code, message);
        _persisted = _persisted with
        {
            State = UpdateState.Failed,
            Candidate = _candidate,
            CandidateIdentity = _candidate?.Identity,
            Download = _download,
            DownloadPath = _download?.Path,
            ErrorCode = code,
            ErrorMessage = message,
        };
        try
        {
            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // The in-memory failure is still useful when durable storage is unavailable.
        }

        return failed;
    }

    private UpdateSnapshot Fail(string code, string message)
    {
        var failed = _snapshot with
        {
            State = UpdateState.Failed,
            ErrorCode = code,
            ErrorMessage = message,
            ShouldNotify = false,
        };
        Publish(failed);
        return failed;
    }

    private async ValueTask PersistAsync(CancellationToken cancellationToken) =>
        await _stateStore.SaveAsync(_persisted, cancellationToken).ConfigureAwait(false);

    private void Publish(UpdateSnapshot snapshot)
    {
        _snapshot = snapshot;
        SnapshotChanged?.Invoke(snapshot);
    }

    private static TimeSpan Min(TimeSpan first, TimeSpan second) =>
        first < second ? first : second;

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UpdateCoordinator));
    }
}
