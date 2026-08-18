# NexGen.MediatR.Extensions.Caching

<p align="center">
  <img src="https://raw.githubusercontent.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/main/assets/images/logo.png" alt="NexGen.MediatR.Extensions.Caching" width="280" />
</p>

<p align="center">
  <strong>MediatR output caching</strong> with pipeline behaviors, tag-based invalidation, and optional Entity Framework auto-eviction.
</p>

<p align="center">
  <a href="https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/MatinGhanbari/NexGen.MediatR.Extensions.Caching/.github%2Fworkflows%2Fci.yml?style=flat-square&label=CI" alt="CI" /></a>
  <a href="https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching"><img src="https://img.shields.io/nuget/v/NexGen.MediatR.Extensions.Caching.svg?style=flat-square" alt="NuGet" /></a>
  <a href="https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching"><img src="https://img.shields.io/nuget/dt/NexGen.MediatR.Extensions.Caching?style=flat-square" alt="Downloads" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square" alt="License" /></a>
  <img src="https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?style=flat-square" alt=".NET 8 | 9 | 10" />
</p>

---

## Table of contents

- [About](#about)
- [Features](#features)
- [Packages](#packages)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Configuration](#configuration)
  - [In-memory cache](#in-memory-cache)
  - [Redis](#redis)
  - [Garnet](#garnet)
  - [Entity Framework auto-evict](#entity-framework-auto-evict)
  - [Distributed eviction (Redis / Garnet)](#distributed-eviction-redis--garnet)
  - [Clear cache on startup](#clear-cache-on-startup)
  - [Cache-hit response header](#cache-hit-response-header)
  - [Conditional caching](#conditional-caching)
- [Production checklist](#production-checklist)
- [Caching requests](#caching-requests)
- [When a response is cached](#when-a-response-is-cached)
- [Invalidation](#invalidation)
- [How it works](#how-it-works)
- [Examples](#examples)
- [Samples and benchmarks](#samples-and-benchmarks)
- [Changelog](#changelog)
- [Contributing](#contributing)
- [Security](#security)
- [License](#license)

---

## About

`NexGen.MediatR.Extensions.Caching` extends [MediatR](https://github.com/jbogard/MediatR) with **opt-in response caching** as a cross-cutting concern. Mark a request with `[RequestOutputCache]`, and a pipeline behavior serves cached responses on hits and stores results on misses.

Invalidation is **tag-based**: associate tags with cached requests, then evict by tag with `[RequestOutputCacheEvict]`, manually, or automatically when Entity Framework Core saves related entity changes. In-memory cache is local to one process. Redis and Garnet add Pub/Sub so other hosts sharing the same cache prefix drop the same tags.

---

## Features

| Feature | Description |
|--------|-------------|
| **Opt-in attribute caching** | Only requests decorated with `[RequestOutputCache]` are cached; unmarked requests pass through unchanged. |
| **MediatR pipeline behavior** | Transparent get / miss / set flow via `RequestOutputCacheBehavior<,>` — no changes inside handlers for cache hits. |
| **Multi-target frameworks** | Ships `net8.0`, `net9.0`, and `net10.0` in one NuGet package set. |
| **In-memory provider** | Built into the core package using `IMemoryCache` for local and development scenarios. |
| **Redis provider** | Distributed cache via `IDistributedCache` + StackExchange.Redis (`NexGen.MediatR.Extensions.Caching.Redis`). |
| **Garnet provider** | Distributed Garnet-compatible provider mirrored with Redis (`NexGen.MediatR.Extensions.Caching.Garnet`). |
| **Tag-based invalidation** | Group related cache entries with tags and evict with `EvictByTagsAsync` or `[RequestOutputCacheEvict]`. |
| **EF Core auto-evict** | On `SaveChanges` / `SaveChangesAsync`, evict tags matching changed entity type **names** (`UseMediatROutputCacheAutoEvict`). |
| **Redis / Garnet Pub/Sub** | Distributed tag eviction across CQRS hosts and microservices (on by default; set `EnableDistributedEviction = false` to opt out). |
| **Command eviction attribute** | `[RequestOutputCacheEvict(tag1, tag2, ...)]` invalidates those tags after a successful handler. |
| **Deterministic cache keys** | Key = `NexGen.MediatR.Extensions:{Namespace:segments}:{TypeName}:{SHA-256(JSON)}` — namespaced, Redis-tree friendly, collision-safe across namespaces. |
| **Per-request expiration** | `expirationInSeconds` on the attribute (default **300**); `0` means no absolute expiration. Provider `DefaultExpirationInSeconds` can replace the library default when the attribute omits an explicit value. |
| **Flush all** | `IRequestOutputCacheInvalidator.FlushAll` clears the entire cache store for the provider. |
| **Clear on startup** | Optional `ClearCacheOnStartup()` during DI configuration. |
| **Cache-hit response header** | On an ASP.NET Core cache hit, sets `X-NexGen-Output-Cache: HIT` (on by default; call `EnableCacheHitResponseHeader(false)` to opt out). |
| **Conditional caching** | Cache a response only when `CacheWhen` is true, or when FluentResults / an `IsSuccess` property reports success. Failed responses are skipped unless `CacheUnsuccessfulResponses(true)` is set. |
| **FluentResults** | Cache get/set/evict APIs return `Result` / `Result<T>` for success and failure paths. |
| **ASP.NET Core DI** | Integrates with `IServiceCollection` and standard Microsoft.Extensions.Caching abstractions. |
| **Enterprise packaging** | Central Package Management, SourceLink, symbol packages (`.snupkg`), XML docs on public APIs. |

---

## Packages

| Package | Role |
|---------|------|
| [`NexGen.MediatR.Extensions.Caching`](https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching) | Core: attribute, behavior, contracts, in-memory provider |
| [`NexGen.MediatR.Extensions.Caching.Redis`](https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching.Redis) | Redis distributed provider |
| [`NexGen.MediatR.Extensions.Caching.Garnet`](https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching.Garnet) | Garnet distributed provider |
| [`NexGen.MediatR.Extensions.Caching.EntityFramework`](https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching.EntityFramework) | EF Core ChangeTracker auto-eviction |

All four packages share the same version (lockstep releases).

---

## Requirements

- **.NET 8**, **.NET 9**, or **.NET 10**
- [MediatR](https://www.nuget.org/packages/MediatR) (registered in your app as usual)
- Optional: Redis/Garnet for distributed cache; EF Core for auto-evict

---

## Installation

### Core

```bash
dotnet add package NexGen.MediatR.Extensions.Caching
```

### Providers (as needed)

```bash
dotnet add package NexGen.MediatR.Extensions.Caching.Redis
dotnet add package NexGen.MediatR.Extensions.Caching.Garnet
dotnet add package NexGen.MediatR.Extensions.Caching.EntityFramework
```

Or via Package Manager Console:

```powershell
Install-Package NexGen.MediatR.Extensions.Caching
```

---

## Quick start

```csharp
// Program.cs
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();
});
```

```csharp
[RequestOutputCache(tags: ["weather"], expirationInSeconds: 300)]
public sealed class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecastDto>>
{
    public int Limit { get; set; } = 10;
}
```

Send the request through MediatR as usual; the first call executes the handler and caches the response. Later identical requests (same type + payload) are served from cache until expiration or eviction.

---

## Configuration

Register **one** cache provider via `AddMediatROutputCache`. Configuring more than one throws.

### In-memory cache

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();
});
```

In-memory cache is **process-local**. `[RequestOutputCacheEvict]` and EF auto-evict run only in this host. Cross-service or split CQRS invalidation is not supported with the memory provider — use Redis or Garnet for that.

Optional provider defaults (applied when the attribute omits an explicit `expirationInSeconds`, i.e. uses the library constant **300**):

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache(o => o.DefaultExpirationInSeconds = 600);
});
```

### Redis

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseRedisCache("localhost:6379,password=YourRedisPassword");
});
```

Provider-specific options (`InstanceName`, `Database`, default TTL, or advanced `ConfigurationOptions`):

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseRedisCache(o =>
    {
        o.ConnectionString = builder.Configuration.GetConnectionString("Redis")!;
        o.InstanceName = "my-app:";
        o.Database = 1;
        o.DefaultExpirationInSeconds = 300;
    });
});
```

> **Multiple apps on one Redis:** set a distinct `InstanceName` (and/or `Database`) per service. A trailing `:` is added automatically if omitted (`"my-app"` → `"my-app:"`). CLR namespaces in response cache keys do **not** isolate the shared container index keys (`…:Container:*`). Without a prefix, apps share that metadata on the same database. Replicas of the *same* service intentionally share one prefix; their concurrent index updates are merged server-side, so they keep each other's entries.

### Garnet

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseGarnetCache("localhost:6379,password=YourGarnetPassword");
});
```

Same nested options pattern as Redis via `UseGarnetCache(Action<GarnetRequestOutputCacheOptions>)`. Use a distinct `InstanceName` / `Database` when multiple apps share one Garnet instance (same guidance as Redis above).

> **TTL precedence:** an explicit `expirationInSeconds` on `[RequestOutputCache]` always wins (including `0` for never expire). Provider `DefaultExpirationInSeconds` only replaces the library default when the attribute uses the constructor default (**300**). Explicit `300` is indistinguishable from that default.

### Entity Framework auto-evict

After a successful `SaveChanges` / `SaveChangesAsync`, the interceptor collects distinct entity CLR type **names** and invalidates those tags. Request tags must match (typically `nameof(YourEntity)`). With Redis or Garnet, the same tags are also published on Pub/Sub when distributed eviction is enabled.

```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.UseMediatROutputCacheAutoEvict(sp);
});
```

### Distributed eviction (Redis / Garnet)

Query and command hosts use the **same** registration. No second DI entry point and no message bus.

```csharp
// every host
builder.Services.AddMediatROutputCache(opt =>
    opt.UseRedisCache(o =>
    {
        o.ConnectionString = builder.Configuration.GetConnectionString("Redis")!;
        o.InstanceName = "my-app:";
        // o.EnableDistributedEviction = true; // default
    }));
```

```csharp
[RequestOutputCache(tags: [nameof(User)], expirationInSeconds: 300)]
public sealed record GetUsersQuery : IRequest<Result<List<UserDto>>>;

[RequestOutputCacheEvict(nameof(User), nameof(Order), "dashboard-stats")]
public sealed record CreateUserCommand(string Name) : IRequest<Result>;
```

| Setting | Behavior |
|---------|----------|
| `EnableDistributedEviction` | Default **true**. Publishes and subscribes on a shared Redis/Garnet channel so other hosts evict the same tags. Set **false** to keep eviction in this process only. |
| `EvictionChannel` | Optional. Defaults to `NexGen.MediatR.Extensions.Caching:Evict`, prefixed by `InstanceName` when set. |
| `InstanceName` | Isolates cache keys **and** the eviction channel so co-tenant apps do not cross-evict. Trailing `:` is ensured automatically. |

The publishing host evicts locally first, then notifies others. Each host ignores its own Pub/Sub echo. Redis Pub/Sub is at-most-once; a missed message is repaired by TTL.

`UseGarnetCache` mirrors the same options (`EnableDistributedEviction`, `EvictionChannel`, `InstanceName`).

### Clear cache on startup

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();
    opt.ClearCacheOnStartup();
});
```

### Cache-hit response header

When a cached MediatR response is served during an ASP.NET Core HTTP request, the pipeline sets:

```http
X-NexGen-Output-Cache: HIT
```

This is **on by default**. Cache misses, unannotated requests, and non-HTTP MediatR executions (console, workers, tests without `HttpContext`) do not set the header.

Opt out:

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();
    opt.EnableCacheHitResponseHeader(false);
});
```

### Conditional caching

By default, a cache miss stores the handler response only when it looks successful (see [When a response is cached](#when-a-response-is-cached)). Register a per-request predicate, or restore the previous “cache everything” behavior:

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();

    opt.CacheWhen<GetOrdersQuery, Result<List<OrderDto>>>(x => x.IsSuccess && x.Value.Count > 0);

    // Restore 2.2.x behavior (cache FluentResults failures and IsSuccess = false as well)
    // opt.CacheUnsuccessfulResponses(true);
});
```

---

## Production checklist

- Use **Redis or Garnet**, not the in-memory provider, when more than one host serves the same queries.
- Give **each service** a distinct `InstanceName` (and/or `Database`) on a shared server, so cache keys, the container indexes (`…:Container:*`), and the eviction channel do not collide with co-tenant apps.
- **Replicas of one service keep the same `InstanceName`.** They share the container indexes on purpose: index updates are merged with a server-side compare-and-swap, so concurrent writers do not drop each other's entries. On a server without scripting support, index writes fall back to last-write-wins.
- Keep an explicit **TTL** on `[RequestOutputCache]`. Redis and Garnet Pub/Sub eviction is at-most-once, and TTL is what repairs a missed message.
- Align EF Core and command tags with entity **`nameof(Entity)`** strings.
- **Flush the cache on deploy** when upgrading across cache key format versions.

---

## Caching requests

Apply `[RequestOutputCache]` on the **request** type (the class that implements `IRequest<TResponse>`).

> **Note:** `TResponse` should be a reference type (class, record, or interface), consistent with typical MediatR query responses.

> **Important:** For EF auto-evict, include `nameof` for every related entity type in `tags`.

```csharp
[RequestOutputCache(
    tags: ["weather", nameof(WeatherForecastDbEntity)],
    expirationInSeconds: 3600)]
public sealed class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecastDto>>
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
}
```

| Attribute parameter | Behavior |
|---------------------|----------|
| `tags` | Labels for grouping and invalidation |
| `expirationInSeconds` | Absolute lifetime in seconds. Default: **300**. Use **`0`** for no absolute expiration |

---

## When a response is cached

On a cache miss the handler always runs. The response is stored only when a cache condition passes. Decision order:

1. **`null` response** — never cached.
2. **`CacheWhen<TRequest, TResponse>`** — if registered, that predicate decides. It receives the whole response (for FluentResults, the `Result` / `Result<T>` instance). Exceptions thrown by the predicate are not caught.
3. Otherwise **FluentResults** `IResultBase.IsSuccess` when the response implements it.
4. Otherwise a public instance `bool IsSuccess` property on the response type, when present.
5. Otherwise the response is cached (same as 2.2.x for types without a success flag).

`CacheUnsuccessfulResponses(true)` skips steps 3–4 so unsuccessful responses are cached again. An explicit `CacheWhen` predicate always takes priority over that flag.

```csharp
builder.Services.AddMediatROutputCache(opt =>
{
    opt.UseMemoryCache();
    opt.CacheWhen<GetUsersQuery, GetUsersResponse>((req, res) => res.Items.Count > 0);
});
```

---

## Invalidation

### Manual (by tags)

Inject `IRequestOutputCacheInvalidator` or `IRequestOutputCache<TRequest, TResponse>`:

```csharp
public sealed class WeatherForecastUpdateHandler(
    IRequestOutputCacheInvalidator cache)
    : IRequestHandler<WeatherForecastUpdateRequest, string>
{
    public async Task<string> Handle(
        WeatherForecastUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await cache.EvictByTagsAsync(["weather"], cancellationToken);
        return "Evicted";
    }
}
```

### Flush everything

```csharp
await cache.FlushAll(cancellationToken);
```

### Automatic (EF Core)

When `UseMediatROutputCacheAutoEvict` is configured, you usually do not need manual eviction for data that changes through that `DbContext`. With Redis or Garnet, the interceptor also notifies other hosts when distributed eviction is enabled.

### Command attribute

Eviction runs only after the handler returns successfully. A thrown exception or a failed FluentResults `IResultBase` skips eviction. Pass any number of tags; they are sent in one call.

```csharp
[RequestOutputCacheEvict(nameof(User), nameof(Order), "dashboard-stats")]
public sealed record CreateUserCommand(string Name) : IRequest<Result>;
```

---

## How it works

```text
[RequestOutputCache]  →  RequestOutputCacheBehavior
                              │
                              ├─ hit  → set X-NexGen-Output-Cache: HIT (HTTP) → return cached TResponse
                              └─ miss → handler → cache only if condition passes → return TResponse

[RequestOutputCacheEvict] → handler succeeds → RequestOutputCacheEvictionDispatcher
                              ├─ local EvictByTagsAsync
                              └─ Redis/Garnet Pub/Sub (when enabled) → other hosts EvictByTagsAsync

Key:   NexGen.MediatR.Extensions:{Namespace:with:colons}:{TypeName}:{sha256(json(request))}
Index: tag → request types → cache keys  (via IRequestOutputCacheContainer)
Evict: EvictByTagsAsync(tags)
       or EF ChangeTracker → entity type Name as tags
```

Distributed providers (Redis / Garnet) also keep request→response type metadata so payloads can be deserialized correctly across nodes.

---

## Examples

### Cache a query

```csharp
[RequestOutputCache(tags: ["weather"], expirationInSeconds: 300)]
public sealed class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecastDto>>
{
    public int Limit { get; set; } = 10;
}

public sealed class WeatherForecastRequestHandler
    : IRequestHandler<WeatherForecastRequest, IEnumerable<WeatherForecastDto>>
{
    public async Task<IEnumerable<WeatherForecastDto>> Handle(
        WeatherForecastRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken); // simulate work
        // ... build and return forecast list
        return [];
    }
}
```

### Invalidate after an update

```csharp
[RequestOutputCacheEvict("weather")]
public sealed class WeatherForecastUpdateRequest : IRequest<string>;
```

Or call the invalidator from a handler:

```csharp
public sealed class WeatherForecastUpdateRequestHandler(
    IRequestOutputCacheInvalidator cache)
    : IRequestHandler<WeatherForecastUpdateRequest, string>
{
    public async Task<string> Handle(
        WeatherForecastUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await cache.EvictByTagsAsync(["weather"], cancellationToken);
        return "Evicted!";
    }
}
```

---

## Samples and benchmarks

| Area | Location |
|------|----------|
| Integration / consumer sample | [`tests/NexGen.MediatR.Extensions.Caching.IntegrationTest`](tests/NexGen.MediatR.Extensions.Caching.IntegrationTest) (includes `docker-compose.yml` for Redis/SQL) |
| Unit tests | [`tests/NexGen.MediatR.Extensions.Caching.UnitTest`](tests/NexGen.MediatR.Extensions.Caching.UnitTest) |
| Benchmarks | [`benchmarks/NexGen.MediatR.Extensions.Caching.Benchmark`](benchmarks/NexGen.MediatR.Extensions.Caching.Benchmark) |

![Benchmark](https://raw.githubusercontent.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/main/assets/images/benchmark.png)

> Larger or more complex responses use more memory with the in-memory provider. Prefer Redis or Garnet for multi-instance and production workloads.

---

## Migrating from 1.x

Version **2.0.0** removes the eviction-bus APIs. Use attributes plus one `AddMediatROutputCache` call.

| 1.x | 2.0 |
|-----|-----|
| `AddMediatROutputCacheEviction` | `AddMediatROutputCache` + `UseRedisCache` / `UseGarnetCache` on every host |
| `UseRedisEvictionBus` / `UseGarnetEvictionBus` | Built into `UseRedisCache` / `UseGarnetCache` (`EnableDistributedEviction`, default `true`) |
| `UseInProcessEvictionBus` / `InProcessRequestOutputCacheEvictionBus` | Removed. Memory cache is process-local only |
| `UseCustomEvictionPublisher` / `Subscriber` / `Bus` | Removed. Use Redis or Garnet Pub/Sub |
| `IRequestOutputCacheEvictionPublisher` / `Subscriber` | `IRequestOutputCacheEvictionNotifier` (provider-internal) + `RequestOutputCacheEvictionDispatcher` |
| `RequestOutputCacheEvictionMessage` | `RequestOutputCacheEvictionNotification` |
| `[RequestOutputCacheEvict]` after any handler return | Evicts only on success (no exception, FluentResults not failed) |

---

## Changelog

Release notes are maintained in **[CHANGELOG.md](CHANGELOG.md)** (Keep a Changelog format). Check that file for what changed in each version.

---

## Contributing

Contributions are welcome through **GitHub Issues** and **Pull Requests**.

Please read **[CONTRIBUTING.md](CONTRIBUTING.md)** for the full contribution guide (development setup, coding standards, tests, and PR expectations) before opening an issue or PR. By participating, you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

---

## Security

Please do not report security vulnerabilities as public issues. See **[SECURITY.md](SECURITY.md)** for supported versions and how to report privately.

---

## License

This project is licensed under the [MIT License](LICENSE).
