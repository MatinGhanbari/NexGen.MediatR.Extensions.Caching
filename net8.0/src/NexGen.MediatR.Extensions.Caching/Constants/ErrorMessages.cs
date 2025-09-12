namespace NexGen.MediatR.Extensions.Caching.Constants;

/// <summary>
/// Contains standard error messages used across the MediatR output caching library.
/// </summary>
public class ErrorMessages
{
    public static readonly string ResponseNotFound = "Response not found";
    public static readonly string CacheHit = "Cache hit for MediatR request {RequestName}, Returning cached response.";
    public static readonly string AlreadyConfigured = "The request output cache has already been configured.";
    public static readonly string EmptyConnectionString = "The connection string cannot be empty.";
}