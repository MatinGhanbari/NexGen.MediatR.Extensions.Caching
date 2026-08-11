# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Package versions for **core**, **Redis**, **Garnet**, and **EntityFramework** are released in **lockstep**.


## [1.4.0] - 2026-08-11

### Changed

- Cache keys now include the request **namespace** (dots → `:` for Redis tree browsing) and the library root prefix `NexGen.MediatR.Extensions`, so entries look like:
  `NexGen.MediatR.Extensions:MyApp:Users:Queries:GetUserQuery:{sha256}`
  and two requests with the same short type name in different namespaces no longer collide (#47).
- Redis / Garnet **container** and **eviction channel** keys use the same root prefix (`NexGen.MediatR.Extensions:Container:*`, `NexGen.MediatR.Extensions:Evict`) instead of `NexGen.MediatR.Caching:*`.
- Public constant `RequestCacheConstants.CacheKeyRootPrefix` documents the shared root.

> **Migration:** existing in-memory / Redis / Garnet entries written with the old key format are not read by this version. Flush or wait for TTL before or after upgrading if you need a clean store.

## [1.3.0] - 2026-08-10

### Added

- Provider-specific configuration overloads for cache registration:
  - `UseMemoryCache(Action<MemoryRequestOutputCacheOptions>)`
  - `UseRedisCache(Action<RedisRequestOutputCacheOptions>)` / `UseGarnetCache(Action<GarnetRequestOutputCacheOptions>)`
  - Optional `InstanceName`, `Database`, `ConfigurationOptions`, and `DefaultExpirationInSeconds`
  - Existing string / parameterless overloads unchanged (delegate to the new APIs)

## [1.3.1] - 2026-08-11

### Changed

- Package metadata and changelog updated for the provider-specific cache configuration overloads shipped in v1.3.0.

## [1.3.0] - 2026-08-10

### Added

- Provider-specific configuration overloads for cache registration:
  - `UseMemoryCache(Action<MemoryRequestOutputCacheOptions>)`
  - `UseRedisCache(Action<RedisRequestOutputCacheOptions>)` / `UseGarnetCache(Action<GarnetRequestOutputCacheOptions>)`
  - Optional `InstanceName`, `Database`, `ConfigurationOptions`, and `DefaultExpirationInSeconds`
  - Existing string / parameterless overloads unchanged (delegate to the new APIs)

## [1.2.0] - 2026-08-10

### Added

- CQRS **eviction bus** for dual DI / split command-query hosts:
  - `IRequestOutputCacheEvictionPublisher` / `IRequestOutputCacheEvictionSubscriber` and `RequestOutputCacheEvictionMessage`
  - `AddMediatROutputCacheEviction` for command-only publisher registration
  - Built-in **in-process** bus (`InProcessRequestOutputCacheEvictionBus`) for co-deployed dual DI
  - Built-in **Redis / Garnet Pub/Sub** buses (`UseRedisEvictionBus` / `UseGarnetEvictionBus`)
  - Pluggable **custom** publisher/subscriber adapters for existing RabbitMQ, Kafka, MassTransit, etc. (`UseCustomEvictionPublisher` / `UseCustomEvictionSubscriber`)
  - `RequestOutputCacheEvictionHostedService` on query hosts to apply bus messages via `EvictByTagsAsync`
  - `[RequestOutputCacheEvict]` attribute and pipeline behavior for non-EF commands
- Suggested bus topic constant `RequestOutputCacheEvictionConstants.DefaultBusTopic` (`mediatr.outputcache.evict`)

### Changed

- EF Core `ChangeTrackerInterceptor` now captures **Added / Modified / Deleted** entity type names before save, prefers publishing on the eviction bus when registered, falls back to local `IRequestOutputCacheInvalidator`, unwraps via `Metadata.ClrType`, and supports sync `SaveChanges` as well as async.

## [1.1.0] - 2026-08-10

### Added

- Multi-targeting for **net8.0**, **net9.0**, and **net10.0** so apps on .NET 8–10 can consume the same package set.
- Central Package Management (`Directory.Packages.props`) and shared library metadata (`src/Directory.Build.props`).
- TFM-aligned Microsoft.Extensions / ASP.NET / EF Core package versions via `Directory.Build.targets`.
- SourceLink, deterministic builds, and symbol packages (`.snupkg`).
- Repository analyzers (NetAnalyzers, Meziantou.Analyzer) and `.editorconfig`.
- `global.json` SDK pin with roll-forward.
- Standard consumer docs: expanded [README.md](README.md), [CONTRIBUTING.md](CONTRIBUTING.md), and this changelog.

### Changed

- Repository layout to `src/`, `tests/`, and `benchmarks/` (removed the `net8.0/` folder).
- CI builds against .NET 8, 9, and 10 and packs all libraries.
- Nullable annotations and small configuration hardening (`RequestOutputCacheConfigurationOption`, container APIs).

## [1.0.8] - 2025

### Added

- Core MediatR output caching with `[RequestOutputCache]` and pipeline behavior.
- In-memory, Redis, and Garnet providers.
- Tag-based eviction (`EvictByTagsAsync`) and `FlushAll`.
- Entity Framework Core ChangeTracker auto-evict (`UseMediatROutputCacheAutoEvict`).
- `ClearCacheOnStartup` configuration option.
- Integration sample and BenchmarkDotNet project.

[Unreleased]: https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/releases/tag/v1.4.0
[Unreleased]: https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/compare/v1.3.1...HEAD
[1.3.1]: https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/releases/tag/v1.3.1
[1.3.0]: https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/releases/tag/v1.3.0
[1.2.0]: https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/releases/tag/v1.2.0
