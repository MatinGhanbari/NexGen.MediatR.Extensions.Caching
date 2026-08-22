using Microsoft.Extensions.Options;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Validates in-memory MediatR output cache provider options at startup.
/// </summary>
internal sealed class MemoryRequestOutputCacheOptionsValidator
    : IValidateOptions<MemoryRequestOutputCacheOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MemoryRequestOutputCacheOptions options)
        => RequestOutputCacheOptionsValidationRules.ValidateDefaultExpiration(options.DefaultExpirationInSeconds);
}
