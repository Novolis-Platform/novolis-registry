# Design

The registry repository owns the reusable catalog API. Static organization
metadata lives in `novolis-governance\registry`; a future online host can
serve the same document without changing the client contracts.

## Goals

- Keep public APIs documented and packable as `Novolis.*` on GitHub Packages.
- Keep catalog primitives independent of HTTP, GitHub, Avalonia, MAUI, and
  installer UI.
- Allow GitHub Releases, GitHub raw content, or a future registry service to
  provide the same catalog contract.
- Respect the closed platform spine and orthogonal islands ([library-boundaries](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md)).

## Package graph

```text
Novolis.Registry.GitHub
├── depends on Novolis.Registry
│   └── depends on Novolis.Registry.Abstractions
│       └── depends on Novolis.Registry.Primitives
└── depends on Novolis.IO.GitHub
```

`Novolis.Registry` contains the provider-neutral `RegistryService` and JSON
handling. `Novolis.Registry.GitHub` only adapts a GitHub-hosted JSON document.
The future server belongs above this graph, for example in
`Novolis.Registry.Hosting.AspNetCore` or a separate executable host.

## Non-goals

- Local NuGet folder feeds or cross-repo `ProjectReference` in committed `.csproj` files.
- Pulling Avalonia into non-`Novolis.Avalonia.*` libraries.
- An online server, authentication, staged rollout, or automatic installer in
  the first library slice.

## Topics

- `dotnet`
- `nuget`
- `registry`
- `novolis`
