using Microsoft.Extensions.Options;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Shared validation rules for MediatR output cache provider options.
/// </summary>
internal static class RequestOutputCacheOptionsValidationRules
{
    internal static ValidateOptionsResult ValidateDefaultExpiration(int? defaultExpirationInSeconds)
    {
        if (defaultExpirationInSeconds is <= 0)
            return ValidateOptionsResult.Fail(Constants.ErrorMessages.InvalidDefaultExpirationInSeconds);

        return ValidateOptionsResult.Success;
    }

    internal static ValidateOptionsResult ValidateDatabase(int? database)
    {
        if (database is < 0)
            return ValidateOptionsResult.Fail(Constants.ErrorMessages.InvalidDatabase);

        return ValidateOptionsResult.Success;
    }
}
