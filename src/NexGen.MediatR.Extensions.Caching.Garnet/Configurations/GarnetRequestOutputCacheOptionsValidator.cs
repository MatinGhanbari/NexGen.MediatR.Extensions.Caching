using Microsoft.Extensions.Options;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Configurations;

/// <summary>
/// Validates Garnet MediatR output cache provider options at startup.
/// </summary>
internal sealed class GarnetRequestOutputCacheOptionsValidator
    : IValidateOptions<GarnetRequestOutputCacheOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, GarnetRequestOutputCacheOptions options)
    {
        var expirationResult = RequestOutputCacheOptionsValidationRules.ValidateDefaultExpiration(
            options.DefaultExpirationInSeconds);
        if (expirationResult.Failed)
            return expirationResult;

        var databaseResult = RequestOutputCacheOptionsValidationRules.ValidateDatabase(options.Database);
        if (databaseResult.Failed)
            return databaseResult;

        if (options.ConfigurationOptions is null && string.IsNullOrWhiteSpace(options.ConnectionString))
            return ValidateOptionsResult.Fail(ErrorMessages.EmptyConnectionString);

        return ValidateOptionsResult.Success;
    }
}
