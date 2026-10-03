# Novolis.Registry.Primitives

Provider-neutral catalog documents, entries, channels, platforms, and update manifests.

## Install

```powershell
dotnet add package Novolis.Registry.Primitives
```

Requires .NET 10.

## Quick start

```csharp
using Novolis.Registry.Primitives.Updates;

var comparison = UpdateVersionComparer.Compare("2026.1.1.4", "2026.1.1.9");
```

`comparison` is negative when the first version is older. Manifest and catalog types in this package stay free of GitHub, Avalonia, and MAUI.
