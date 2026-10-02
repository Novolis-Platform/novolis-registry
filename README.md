<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-registry.svg" width="100%" alt="novolis-registry"/>
</p>

<p align="center">
  <strong>Registry contracts and providers</strong><br/>
  Provider-neutral catalog primitives, services, and GitHub integration.
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-registry/"><img src="https://img.shields.io/badge/docs-portfolio-0a7ea3" alt="docs"/></a>
  <a href="https://github.com/Novolis-Platform/novolis-registry/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-registry/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-registry"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-registry/">Docs</a>
  ·
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
# novolis-registry

`novolis-registry` is now the library repository for the Novolis registry
domain. It contains provider-neutral catalog models and service contracts,
plus a GitHub-backed source implementation.

The static organization catalog is maintained in
[`novolis-governance/registry`](https://github.com/Novolis-Platform/novolis-governance/tree/main/registry).
This repository does not host application binaries or act as the future
online registry server.

## Packages

| Package | Responsibility |
|---------|----------------|
| `Novolis.Registry.Primitives` | Catalog documents, entries, channels, platforms, and artifacts |
| `Novolis.Registry.Abstractions` | Source and service contracts |
| `Novolis.Registry` | JSON handling and provider-neutral `RegistryService` |
| `Novolis.Registry.GitHub` | GitHub raw-content source using `Novolis.IO.GitHub` |

## Dependency graph

```text
Novolis.Registry.Primitives
        ↑
Novolis.Registry.Abstractions
        ↑
Novolis.Registry
        ↑
Novolis.Registry.GitHub ──> Novolis.IO.GitHub
```

The future online host should be a separate
`Novolis.Registry.Hosting.AspNetCore` surface or executable. It can expose
`RegistryService` without forcing ASP.NET Core into the core packages.

## Build

```powershell
dotnet build d:\novolis\novolis-registry\Novolis.Registry.slnx
dotnet test d:\novolis\novolis-registry\Novolis.Registry.slnx
```

Packages publish through the normal Novolis GitHub Packages workflow. Cross
repository dependencies use `LibraryReference`/NuGet resolution; committed
sibling `ProjectReference` paths are not used.

## Related

- [Static registry data](https://github.com/Novolis-Platform/novolis-governance/tree/main/registry)
- [Novolis.IO.GitHub](https://github.com/Novolis-Platform/novolis-io)
- [Novolis.Install](https://github.com/Novolis-Platform/novolis-tools/tree/main/src/Novolis.Install)

