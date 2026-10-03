# Novolis.Registry.Abstractions

Source and service contracts for package and application catalogs.

## Install

```powershell
dotnet add package Novolis.Registry.Abstractions
```

Requires .NET 10 and `Novolis.Registry.Primitives`.

## Quick start

```csharp
using Novolis.Registry.Abstractions;

IRegistrySource source = /* host-owned catalog source */;
var document = await source.LoadAsync();
```

Hosts implement `IRegistrySource`. `IRegistryService` is the lookup surface over a loaded catalog.
