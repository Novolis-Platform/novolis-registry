# Novolis.Registry.Updates

Provider-neutral coordination for applications that discover updates from a
direct-release source. It owns polling cadence, ETags, retry backoff, channel
and target selection seams, durable state, snoozing, verified downloads, and
platform handoff contracts. It does not depend on GitHub, Avalonia, MAUI, or a
specific installer.

## Install

```powershell
dotnet add package Novolis.Registry.Updates
```

Requires .NET 10 and `Novolis.Registry.Primitives`.

## Quick start

```csharp
var coordinator = new UpdateCoordinator(
    request,
    source,
    stateStore,
    downloader,
    platformAction);

var snapshot = await coordinator.CheckAsync();
if (snapshot.Candidate is not null)
{
    await coordinator.DownloadAsync();
    await coordinator.ApplyAsync();
}
```

Use `Novolis.Registry.GitHub` for the source and provide host-owned state,
download storage, and platform action implementations. `ApplyAsync` is a
handoff only; hosts decide whether an installer or platform package
confirmation is required.

Store-managed requests are rejected before a direct source is contacted.
Artifacts are promoted only after their expected byte length and SHA-256
digest have been verified.

## Related packages

| Package | Role |
| --- | --- |
| `Novolis.Registry.Primitives` | Versioned manifest and update contracts |
| `Novolis.Registry.GitHub` | GitHub Releases provider |
| `Novolis.Avalonia.Updates` | Avalonia status/notification surface |
| `Novolis.Maui.Updates` | MAUI status/notification surface |

## Support

Direct-release updater coordination; pre-release API.
