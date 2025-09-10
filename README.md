# NexGen.MediatR.Extensions.Caching

[![Build And Test](https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/actions/workflows/build.yml/badge.svg)](https://github.com/MatinGhanbari/NexGen.MediatR.Extensions.Caching/actions/workflows/build.yml)

A lightweight and flexible library that extends [MediatR](https://github.com/jbogard/MediatR) to provide seamless caching and cache invalidation for requests using pipeline behaviors in .NET applications. This library integrates caching as a cross-cutting concern, enabling developers to cache query results and invalidate caches efficiently within the MediatR pipeline, improving application performance and scalability.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Configuration](#configuration)
  - [Configure MediatR and Caching Services](#step-1-configure-mediatr-and-caching-services)
  - [Using Caching Services](#step-2-using-caching-services)
  - [Invalidate Cached Responses](#step-3-invalidate-cached-responses)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Seamless Integration**: Adds caching to MediatR requests using pipeline behaviors, keeping your codebase clean and maintainable.
- **Flexible Cache Storage**: Supports both in-memory (`IMemoryCache`) and distributed caching (`IDistributedCache`) out of the box.
- **Automatic Cache Invalidation**: Provides pipeline behaviors to invalidate cached requests based on other requests or notifications.
- **Customizable Cache Options**: Configure cache expiration, sliding expiration, and cache keys per request.
- **ASP.NET Core Compatibility**: Works effortlessly with ASP.NET Core's dependency injection and caching infrastructure.
- **Extensible Design**: Easily extend or customize caching behavior to suit your application's needs.

## Installation

You can install `NexGen.MediatR.Extensions.Caching` via NuGet Package Manager or the .NET CLI.

### Using NuGet Package Manager

```bash
Install-Package NexGen.MediatR.Extensions.Caching
```

### Using .NET CLI

```bash
dotnet add package NexGen.MediatR.Extensions.Caching
```

Ensure you have the following dependencies installed:

- `MediatR` (version compatible with your project)
- `Microsoft.Extensions.Caching.Abstractions` (for in-memory or distributed caching)
- `Microsoft.Extensions.DependencyInjection` (for ASP.NET Core DI)

## Configuration

To use `NexGen.MediatR.Extensions.Caching`, you need to configure MediatR and the caching services in your application's dependency injection container.

### Step 1: Configure MediatR and Caching Services

In your `Startup.cs` or `Program.cs` (for ASP.NET Core), register MediatR and the caching services:

- Setup using `MemoryCache`

  ```csharp
  builder.Services.AddMediatROutputCache(opt =>
  {
      opt.UseMemoryCache();
  });
  ```

- Setup using `Redis` (`NexGen.MediatR.Extensions.Caching.Redis`)
  ```csharp
  builder.Services.AddMediatROutputCache(opt =>
  {
      var redisConnectionString = "localhsot:6379,password=YourPassword";
      opt.UseRedisCache(redisConnectionString);
  });
  ```

If you're using a different DI container (e.g., Autofac), refer to the container's documentation for registering MediatR and caching services.

### Step 2: Using Caching Services

Add `RequestOutputCache` attribute your Request class that implements `IRequest` interface. Example:

```csharp
[RequestOutputCache(tags: ["weather"], expirationInSeconds: 300)]
public class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecastDto>>
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
}
```

### Step 3: Invalidate Cached Responses

To **Invalidate** the cached responses you should evict them by their tags. Example:

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

## Examples

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

> NOTE: See Integration Test in test folder to see for example.

## Contributing

Contributions are welcome! To contribute to `NexGen.MediatR.Extensions.Caching`:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature/your-feature`).
3. Make your changes and commit them (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Open a pull request.

Please ensure your code follows the project's coding standards and includes unit tests where applicable.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
