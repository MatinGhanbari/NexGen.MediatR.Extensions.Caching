# Contributing to NexGen.MediatR.Extensions.Caching

Thank you for your interest in contributing. This document explains how to report issues, propose changes, and submit pull requests.

## Ways to contribute

- **Issues** — report bugs, ask questions, or request features on [GitHub Issues](https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/issues).
- **Pull requests** — propose code, documentation, or test improvements via [Pull Requests](https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/pulls).

Please search existing issues and PRs before opening a new one to avoid duplicates.

## Before you start

1. Read the [README](README.md) for product behavior and public APIs.
2. Skim [CHANGELOG.md](CHANGELOG.md) for recent releases.
3. Treat every `public` type and member as a **consumer contract** (SemVer). Prefer additive APIs over breaking changes.

## Development setup

### Prerequisites

- .NET SDK **10.x** (see [`global.json`](global.json); roll-forward is enabled)
- Targeting packs / runtimes for **net8.0**, **net9.0**, and **net10.0** (CI installs all three)
- Optional: Docker for the integration sample (Redis / SQL Server) — see `tests/NexGen.MediatR.Extensions.Caching.IntegrationTest/docker-compose.yml`
- Optional: Docker for Redis / Garnet provider benchmarks — see `benchmarks/docker-compose.yml`

### Build and test

```bash
dotnet restore
dotnet build NexGen.MediatR.Extensions.Caching.sln -c Release
dotnet test NexGen.MediatR.Extensions.Caching.sln -c Release --no-build
```

Pack libraries locally:

```bash
dotnet pack src/NexGen.MediatR.Extensions.Caching/NexGen.MediatR.Extensions.Caching.csproj -c Release -o ./artifacts
```

### Running benchmarks

```powershell
.\benchmarks\run-benchmarks.ps1                 # all suites (starts Redis + Garnet if needed)
.\benchmarks\run-benchmarks.ps1 -Suite pipeline # memory pipeline only, no Docker
```

```bash
./benchmarks/run-benchmarks.sh
./benchmarks/run-benchmarks.sh pipeline
```

Suites: `all`, `pipeline`, `micro`, `eviction`, `provider`. Results are machine-specific; the checked-in summary lives in [docs/BENCHMARKS.md](docs/BENCHMARKS.md). Pass `-KeepContainers` (PowerShell) or `KEEP_CONTAINERS=1` (bash) to leave Redis/Garnet running.

## Repository layout

| Path | Purpose |
|------|---------|
| `src/` | Packable libraries (core, Redis, Garnet, EntityFramework) |
| `tests/` | Unit tests and ASP.NET integration sample |
| `benchmarks/` | BenchmarkDotNet projects and `docker-compose.yml` for Redis/Garnet |
| `docs/BENCHMARKS.md` | Checked-in benchmark results and methodology |
| `Directory.Packages.props` | Central NuGet dependency versions |
| `src/Directory.Build.props` | Lockstep package `Version` and NuGet metadata |
| `Directory.Build.targets` | TFM-aligned Microsoft / EF package versions |

## Coding guidelines

- Match the style of the file you edit; avoid unrelated refactors.
- Keep namespaces aligned with folder structure under each project.
- Document public APIs with XML `<summary>` (and params/returns when useful).
- Prefer `.ConfigureAwait(false)` on library async I/O.
- Cache get/set/evict use **FluentResults** (`Result` / `Result<T>`), not exceptions for miss/fail paths.
- **Redis and Garnet** should stay feature-parity unless you intentionally diverge — update both when changing shared provider behavior.
- Do not put Redis- or EF-specific types into the core project.

## Pull request process

1. Fork the repository (or create a branch if you have write access).
2. Create a focused branch (`feature/...`, `fix/...`, `docs/...`).
3. Implement the change with tests for new or changed public behavior.
4. Update docs when consumer-facing:
   - [README.md](README.md) — setup, features, or public API usage
   - [CHANGELOG.md](CHANGELOG.md) — under `## [Unreleased]` (Keep a Changelog)
5. Ensure `dotnet build` and `dotnet test` succeed locally.
6. Open a pull request with a clear description of **why** the change is needed and **how** to verify it.

### Versioning

Package versions are set once in `src/Directory.Build.props` and stay lockstep across all four packages. Maintainers bump the version for releases; contributors usually only add notes under `[Unreleased]` in the changelog unless asked otherwise. Pushes to `main` with a new version publish all four packages to nuget.org and GitHub Packages, then create a GitHub Release.

## Reporting bugs

Include:

- Package name(s) and version
- Target framework (net8.0 / net9.0 / net10.0)
- Provider (Memory / Redis / Garnet) and whether EF auto-evict is enabled
- Minimal reproduction or clear steps
- Expected vs actual behavior

## Feature requests

Describe the use case, how it fits the current opt-in / tag-based model, and whether it would be additive or breaking for existing consumers.

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). By participating, you agree to uphold it.

## Security

Do not open a public issue for vulnerabilities. Follow **[SECURITY.md](SECURITY.md)** and report privately via GitHub Security Advisories.

## License

By contributing, you agree that your contributions will be licensed under the same [MIT License](LICENSE) as the project.
