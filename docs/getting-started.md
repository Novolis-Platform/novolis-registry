# Getting started

Build the registry library repository and its provider adapter.

Published guide: [https://novolis-platform.github.io/.github/novolis-registry/](https://novolis-platform.github.io/.github/novolis-registry/)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- GitHub Packages auth for `Novolis.*` (see [nuget-only-policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/nuget-only-policy.md))

Configure GPR once from a sibling `novolis-governance` checkout:

```powershell
pwsh -File d:\novolis\novolis-governance\scripts\configure-gpr-user-nuget.ps1
```

## Build and test

```powershell
dotnet build d:\novolis\novolis-registry\Novolis.Registry.slnx
dotnet test d:\novolis\novolis-registry\Novolis.Registry.slnx
```

The GitHub adapter consumes `Novolis.IO.GitHub` through `LibraryReference`.
Local multi-repo iteration uses ProjectReference mode via
`d:\novolis\Novolis.Platform.slnx`; it never uses a local NuGet folder feed.

## Packages

```text
Novolis.Registry.Primitives
Novolis.Registry.Abstractions
Novolis.Registry
Novolis.Registry.GitHub
```

The static catalog data is maintained in
`d:\novolis\novolis-governance\registry`.

## Next

- [design.md](design.md) — layer placement and non-goals
- [release.md](release.md) — publish cadence
- [Org docs catalog](https://novolis-platform.github.io/.github/)
