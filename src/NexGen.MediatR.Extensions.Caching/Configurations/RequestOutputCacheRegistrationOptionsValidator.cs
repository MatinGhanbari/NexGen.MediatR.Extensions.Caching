using Microsoft.Extensions.Options;
using NexGen.MediatR.Extensions.Caching.Constants;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Validates that a cache provider was registered for MediatR output caching.
/// </summary>
internal sealed class RequestOutputCacheRegistrationOptionsValidator
    : IValidateOptions<RequestOutputCacheRegistrationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RequestOutputCacheRegistrationOptions options)
    {
        if (options.CacheType == default)
            return ValidateOptionsResult.Fail(ErrorMessages.CacheProviderNotSelected);

        return ValidateOptionsResult.Success;
    }
}
