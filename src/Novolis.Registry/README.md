# Novolis.Registry

JSON handling and the provider-neutral `RegistryService`.

## Install

```powershell
dotnet add package Novolis.Registry
```

Requires .NET 10, `Novolis.Registry.Primitives`, and `Novolis.Registry.Abstractions`.

## Quick start

```csharp
using Novolis.Registry;

var service = new RegistryService(source);
var entry = await service.FindAsync("novolis.math");
```

`source` is an `IRegistrySource`. The service loads the catalog and resolves entries. It does not know which host produced the document.
