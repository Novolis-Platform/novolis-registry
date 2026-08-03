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
  <strong>Static package & app catalog</strong><br/>
  Static registry of Novolis packages and apps.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-registry/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-registry/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-registry"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
# novolis-registry

Static **package registry** for the Novolis ecosystem — one JSON file per published NuGet package. Tooling, org landing pages, and migration runbooks reference these entries to discover package ids, repos, and versions.

This repo does **not** host binaries. Packages install from **nuget.org** and **GitHub Packages** (`Novolis.*` at `2026.1.*`).

## Layout

```text
novolis-registry/
  packages/           # one *.json per package (kebab-case file name)
  schemas/
    package.schema.json
  logo-icon.svg
```

Each file under `packages/` describes a single NuGet package, for example `packages/novolis.simulation.json`:

```json
{
  "id": "novolis.simulation",
  "name": "Novolis.Simulation",
  "type": "nuget",
  "version": "2026.1.1.0",
  "repository": "https://github.com/Novolis-Platform/novolis-simulation",
  "packageId": "Novolis.Simulation"
}
```

| Field | Meaning |
|-------|---------|
| `id` | Registry slug (lowercase, dot-separated) |
| `name` | Display / assembly name |
| `type` | `"nuget"` for platform libraries |
| `version` | Last known published version (update on release) |
| `repository` | Source repo URL |
| `packageId` | NuGet `PackageId` for `dotnet add package` |

Optional schema fields (`sha256`, `downloadUrl`) apply when pointing at downloadable artifacts; most Novolis packages resolve via NuGet feeds instead.

## Adding or updating an entry

1. Publish the package from its repo (merge to `main` → CI → GitHub Packages / nuget.org).
2. Create or edit `packages/<package-id-kebab>.json` — kebab-case mirrors the NuGet id (`Novolis.Economy.Core` → `novolis.economy.core.json`).
3. Set `version` to the released `2026.1.*` build and `repository` to the GitHub repo.
4. Open a PR in `novolis-registry`; validate against `schemas/package.schema.json`.

Template and migration steps: [frank-migration-runbook.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/frank-migration-runbook.md) (Registry entry section).

## Consumers

| Consumer | Use |
|----------|-----|
| Org landing / status scripts | Package inventory matrices |
| Governance import plans | Track migration from Frank.* repos |
| Maintainers | Single index of what exists and where it lives |

There is no separate apps index in this repo today — application dogfood lives in [`novolis-dogfooding`](https://github.com/Novolis-Platform/novolis-dogfooding).

## Policy

Do not add local NuGet folder feeds here. Registry entries point at org feeds only (nuget.org + GitHub Packages).

