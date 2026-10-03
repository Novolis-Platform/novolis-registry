# Novolis.Registry.GitHub

GitHub raw-content source for a registry catalog, and a GitHub Releases source for direct-release update manifests.

## Install

```powershell
dotnet add package Novolis.Registry.GitHub
```

Requires .NET 10, `Novolis.Registry`, `Novolis.Registry.Updates`, and `Novolis.IO.GitHub`.

## Quick start

```csharp
using Novolis.Registry;
using Novolis.Registry.GitHub;

var source = new GitHubRegistrySource(httpClient, new GitHubRegistrySourceOptions
{
    Repository = repository,
    Path = "registry/index.json",
});
var entry = await new RegistryService(source).FindAsync("novolis.math");
```

`repository` is a `Novolis.IO.GitHub.GitHubRepository`. `GitHubUpdateSource` reads `Novolis.Update.json` from a release for the direct-release updater.
