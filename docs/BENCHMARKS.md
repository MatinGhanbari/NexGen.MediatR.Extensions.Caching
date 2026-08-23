# Benchmarks

Numbers below were captured on **2026-08-22** with BenchmarkDotNet **0.15.2**, a short job (`WarmupCount=3`, `IterationCount=8`), and `.NET 10.0.10` (`net10.0`).

| | |
|---|---|
| Machine | Windows 11 (10.0.26200), Intel Core i7-10870H @ 2.20 GHz, 16 logical / 8 physical cores |
| Runtime | .NET SDK 10.0.302, X64 RyuJIT AVX2 |
| Redis | Docker `redis:7-alpine` on `localhost:6379` (password auth) |
| Garnet | Docker `ghcr.io/microsoft/garnet` on `localhost:6380` |

Results are machine-specific. Re-run locally with [`benchmarks/run-benchmarks.ps1`](../benchmarks/run-benchmarks.ps1) (or [`benchmarks/run-benchmarks.sh`](../benchmarks/run-benchmarks.sh)) before treating these figures as a regression baseline.

## Methodology

The previous benchmark compared a cached MediatR request against an uncached one whose handler called `Task.Delay(200–600ms)` and built a new request (with a random field) on every invocation. That mixed handler cost into the measurement and usually missed the cache.

This suite instead:

- Uses **deterministic request values** so a warmed entry is a real cache hit.
- Uses a **trivial handler** (`Task.FromResult("result")`) so the numbers show library overhead, not simulated I/O.
- Splits **hit**, **miss**, **set**, and **no-attribute** paths.
- Measures tag eviction and cache-key generation as separate suites.
- Compares **Memory**, **Redis**, and **Garnet** Get/Set against Docker-backed servers.

Caching still pays off when the handler does real work (database, HTTP, CPU). These figures answer “how much does the library itself cost?”

## Pipeline (in-memory)

`IMediator.Send` with `UseMemoryCache()`. `CacheSet` calls `IRequestOutputCache.SetAsync` directly (write path, no Get).

| Method | Mean | Error | Allocated | vs `NoAttribute` |
|--------|-----:|------:|----------:|-----------------:|
| `NoAttribute` | 829.8 ns | 33.4 ns | 784 B | 1.00× |
| `CacheSet` | 2.38 µs | 0.26 µs | 3,160 B | 2.87× |
| `CacheHit` | 4.09 µs | 0.19 µs | 3,776 B | 4.93× |
| `CacheMiss` | 11.22 µs | 1.20 µs | 7,816 B | 13.52× |

A hit is about **2.7× faster** than a miss on this workload. A hit is also about **5×** an uncached MediatR send because every lookup still serializes the request, hashes the cache key (JSON + SHA-256), and allocates. That overhead is small compared with a typical query handler.

## Cache key generation

`RequestOutputCacheHelper.GetCacheKey` (JSON serialize + SHA-256). `SmallRequest` is a one-property record; `VariableRequest` is a dictionary with `PropertyCount` entries.

| Method | PropertyCount | Mean | Allocated |
|--------|--------------:|-----:|----------:|
| `SmallRequest` | 1 / 10 / 100 | ~1.68 µs | 2.42 KB |
| `VariableRequest` | 1 | 1.94 µs | 2.70 KB |
| `VariableRequest` | 10 | 2.51 µs | 3.10 KB |
| `VariableRequest` | 100 | 10.52 µs | 9.22 KB |

Key generation cost grows with request payload size. Larger responses also increase memory use on the in-memory provider (see the README note); prefer Redis or Garnet when entries are large or the process is multi-instance.

## In-memory container

Direct calls on `RequestOutputCacheContainer` (no MediatR). `EntryCount` is the number of seeded keys / tags.

| Method | EntryCount | Mean | Allocated |
|--------|-----------:|-----:|----------:|
| `RemoveRequestTypes` | 1 | 12.18 µs | 984 B |
| `RemoveRequestTypes` | 10 | 11.55 µs | 984 B |
| `RemoveRequestTypes` | 100 | 10.03 µs | 984 B |
| `UpdateContainer` | 1 | 14.95 µs | 864 B |
| `UpdateContainer` | 10 | 13.71 µs | 3,224 B |
| `UpdateContainer` | 100 | 33.02 µs | 28,264 B |

Removing a request type is effectively O(1) in allocated bytes here (the type is dropped as a unit). Updating the container with many tags allocates with tag count.

## Tag eviction (in-memory)

`IRequestOutputCacheInvalidator.EvictByTagsAsync` after seeding `EntryCount` entries per tag (`User`, `Order`, `Product`).

| Method | EntryCount | Mean | Allocated |
|--------|-----------:|-----:|----------:|
| `EvictSingleTag` (`User`) | 10 | 17.75 µs | 1.55 KB |
| `EvictManyTags` (3 tags) | 10 | 22.24 µs | 3.90 KB |
| `EvictSingleTag` | 100 | 26.03 µs | 1.55 KB |
| `EvictManyTags` | 100 | 60.77 µs | 3.90 KB |
| `EvictSingleTag` | 1,000 | 187.12 µs | 1.55 KB |
| `EvictManyTags` | 1,000 | 503.03 µs | 3.90 KB |

Allocation stays flat as entry count grows; time scales with how many keys are removed.

## Providers: GetHit / SetMiss

Same `IRequestOutputCache` Get/Set API. Redis and Garnet ran in Docker on localhost. Distributed eviction Pub/Sub was disabled so the numbers reflect store Get/Set plus the tag index.

| Provider | Method | Mean | Median | Allocated |
|----------|--------|-----:|-------:|----------:|
| Memory | `GetHit` | 1.92 µs | — | 2.53 KB |
| Memory | `SetMiss` | 5.23 µs | — | 3.27 KB |
| Redis | `GetHit` | 1.54 ms | 1.51 ms | 11.73 KB |
| Redis | `SetMiss` | 21.74 ms | 15.26 ms | 704 KB |
| Garnet | `GetHit` | 1.84 ms | 1.87 ms | 11.73 KB |
| Garnet | `SetMiss` | 22.75 ms | 12.07 ms | 1,493 KB |

Notes:

- Memory hits stay in-process (no JSON, no network). Redis/Garnet hits pay serialization plus Docker localhost round-trips (~1.5–1.9 ms here).
- `SetMiss` for Redis/Garnet rewrites the distributed tag index on every unique key. Mean and allocated bytes rise as the index grows during the job; **median** is the better point estimate. This is expected index behavior, not a fair micro-benchmark of a single `SET`.
- Prefer Redis or Garnet for multi-instance deployments, not because they are faster in-process than `IMemoryCache`.

## How to run

From the repository root (Docker required for the provider suite):

```powershell
.\benchmarks\run-benchmarks.ps1              # all suites
.\benchmarks\run-benchmarks.ps1 -Suite pipeline
.\benchmarks\run-benchmarks.ps1 -Suite micro
.\benchmarks\run-benchmarks.ps1 -Suite eviction
.\benchmarks\run-benchmarks.ps1 -Suite provider
```

```bash
./benchmarks/run-benchmarks.sh
./benchmarks/run-benchmarks.sh pipeline
```

`pipeline`, `micro`, and `eviction` do not need Docker. `provider` / `all` start Redis (`6379`) and Garnet (`6380`) via [`benchmarks/docker-compose.yml`](../benchmarks/docker-compose.yml) when those ports are not already open. Use `-SkipDocker` (PowerShell) or `SKIP_DOCKER=1` (bash) to skip compose; Redis/Garnet jobs are skipped if the ports are closed.

```bash
dotnet run -c Release --project benchmarks/NexGen.MediatR.Extensions.Caching.Benchmark -- all
```
