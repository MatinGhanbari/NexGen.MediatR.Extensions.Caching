# ⚡ NexGen.MediatR.Extensions.Caching

![CI](https://raw.githubusercontent.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/main/assets/images/logo.png)

A lightweight and flexible library that extends [MediatR](https://github.com/jbogard/MediatR) to provide seamless caching and cache invalidation for requests using pipeline behaviors in .NET applications.  
This library integrates caching as a cross-cutting concern, enabling developers to cache query results 🚀 and invalidate caches efficiently within the MediatR pipeline, improving application performance and scalability.

[![CI](https://img.shields.io/github/actions/workflow/status/MatinGhanbari/NexGen.MediatR.Extensions.Caching/.github%2Fworkflows%2Fci.yml?style=for-the-badge)](https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/NexGen.MediatR.Extensions.Caching.svg?style=for-the-badge)](https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching)
[![NuGet](https://img.shields.io/nuget/dt/NexGen.MediatR.Extensions.Caching?style=for-the-badge)](https://www.nuget.org/packages/NexGen.MediatR.Extensions.Caching)

## 📑 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Configuration](#%EF%B8%8F-configuration)
  - [Configure MediatR and Caching Services](#step-1-configure-mediatr-and-caching-services)
  - [Using Caching Services](#step-2-using-caching-services)
  - [Invalidate Cached Responses](#step-3-invalidate-cached-responses)
- [Examples](#-examples)
- [Contributing](#-contributing)
- [License](#-license)

## ✨ Features

- **Seamless Integration**: Adds caching to MediatR requests using pipeline behaviors.
- **Flexible Cache Storage**: Supports both in-memory (`IMemoryCache`) 💾 and distributed caching (`IDistributedCache`) 🌐.
- **Automatic Cache Invalidation**: Invalidate cached requests based on other requests or notifications.
- **Customizable Cache Options**: Configure expiration ⏳, sliding expiration, and cache keys per request.
- **ASP.NET Core Compatibility**: Works with ASP.NET Core’s DI and caching infrastructure.
- **Extensible Design**: Easily extend or customize caching behavior to suit your needs.

## 📦 Installation

You can install `NexGen.MediatR.Extensions.Caching` via NuGet Package Manager or the .NET CLI.

### Using NuGet Package Manager

```bash
Install-Package NexGen.MediatR.Extensions.Caching
```

### Using .NET CLI

```bash
dotnet add package NexGen.MediatR.Extensions.Caching
```

## ⚙️ Configuration

### Step 1: Configure MediatR and Caching Services

In your `Startup.cs` or `Program.cs`, register MediatR and caching:

- Using `MemoryCache`

  ```csharp
  builder.Services.AddMediatROutputCache(opt =>
  {
      opt.UseMemoryCache();
  });
  ```

- Using `Redis` (`NexGen.MediatR.Extensions.Caching.Redis`)

  ```csharp
  builder.Services.AddMediatROutputCache(opt =>
  {
      var redisConnectionString = "localhost:6379,password=YourPassword";
      opt.UseRedisCache(redisConnectionString);
  });
  ```

### Step 2: Using Caching Services

Add `RequestOutputCache` attribute to your `IRequest` class:
> [!Note]
> The request class must implement `IRequest<TResponse>` where TResponse is class, record or interface (the mediator request format)!

```csharp
[RequestOutputCache(tags: ["weather"], expirationInSeconds: 300)]
public class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecastDto>>
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
}
```

### Step 3: Invalidate Cached Responses

Invalidate cached responses by tags:

```csharp
public class TestClass
{
    private readonly IRequestOutputCache<WeatherForecastEvictRequest, string> _cache;

    public TestClass(IRequestOutputCache<WeatherForecastEvictRequest, string> cache)
    {
        _cache = cache;
    }

    public async Task EvictWeatherResponses()
    {
        List<string> tags = [ "weather" ];
        await _cache.EvictByTagsAsync(tags);
    }
}
```

## 💡 Examples

### Example 1: Caching a List of Items

Suppose you have a query to fetch a list of Weather Forecasts:

```csharp
[RequestOutputCache(tags: ["weather"], expirationInSeconds: 300)]
public class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecastDto>>
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
}

public class WeatherForecastRequestHandler : IRequestHandler<WeatherForecastRequest, IEnumerable<WeatherForecastDto>>
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<IEnumerable<WeatherForecastDto>> Handle(WeatherForecastRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);
        return Enumerable.Range(1, request.Limit).Select(index => new WeatherForecastDto
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
}
```

The response will be cached with the key `"weather"` for 5 minutes.

> [!IMPORTANT]  
> If `expirationInSeconds` is not provided, it uses the **default value**. To make the response never expire, set `expirationInSeconds` to **Zero**.

### Example 2: Invalidation on Update

When a response must be updated, invalidate the product list cache:

```csharp
public class WeatherForecastUpdateRequest : IRequest<string>
{
}

public class WeatherForecastUpdateRequestHandler : IRequestHandler<WeatherForecastUpdateRequest, string>
{
    private readonly IRequestOutputCache<WeatherForecastUpdateRequest, string> _cache;

    public WeatherForecastUpdateRequestHandler(IRequestOutputCache<WeatherForecastUpdateRequest, string> cache)
    {
        _cache = cache;
    }

    public async Task<string> Handle(WeatherForecastUpdateRequest request, CancellationToken cancellationToken)
    {
        var tags = new List<string> { nameof(WeatherForecastDto) };
        await _cache.EvictByTagsAsync(tags, cancellationToken);

        return "Evicted!";
    }
}
```

> [!NOTE]
> See Integration Test in test folder to see for example.

## 📈 Benchmarks

![Benchmark](https://raw.githubusercontent.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/main/assets/images/benchmark.png)

> [!NOTE]
> This benchmark is available in benchmark directory (`NexGen.MediatR.Extensions.Caching.Benchmark`).

> [!TIP]
> This is benchmark results of testing same simple request with and without caching using `NexGen.MediatR.Extensions.Caching` package.
> The bigger and complicated responses may use more allocated memory in memory cache solution.
> Better to use distributed cache services like `Redis` in enterprise projects.

## 🤝 Contributing

Contributions are welcome! To contribute to `NexGen.MediatR.Extensions.Caching`:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature/your-feature`).
3. Make your changes and commit them (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Open a pull request.

Please ensure your code follows the project's coding standards and includes unit tests where applicable.

## 📃 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
