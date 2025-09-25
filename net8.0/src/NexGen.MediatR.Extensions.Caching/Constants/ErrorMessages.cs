using FluentResults;

namespace NexGen.MediatR.Extensions.Caching.Constants;

/// <summary>
/// Contains standard error messages used across the MediatR output caching library.
/// </summary>
public static class ErrorMessages
{
    public const string ResponseNotFound = "Response not found.";
    public const string CacheHit = "Cache hit for MediatR request {RequestName}, Returning cached response.";
    public const string AlreadyConfigured = "The request output cache has already been configured.";
    public const string EmptyConnectionString = "The connection string cannot be empty.";
    public const string ContainerUpdatesFails = "Container Updates Fails.";
    public const string FailedToUpdateContainer = "Failed to update the cache container.";
    public const string UnableToEvictEntitiesOnDbSaveChange = "Unable to evict entities on entity framework database save change.";
}