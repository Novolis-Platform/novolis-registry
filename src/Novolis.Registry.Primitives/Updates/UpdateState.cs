namespace Novolis.Registry.Primitives.Updates;

/// <summary>Observable state of an update coordinator.</summary>
public enum UpdateState
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Deferred,
    Downloading,
    DownloadReady,
    Failed,
}
