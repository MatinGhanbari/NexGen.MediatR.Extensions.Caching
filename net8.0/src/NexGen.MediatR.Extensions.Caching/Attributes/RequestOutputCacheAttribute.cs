using NexGen.MediatR.Extensions.Caching.Constants;

namespace NexGen.MediatR.Extensions.Caching.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RequestOutputCacheAttribute : Attribute
{
    public string[] Tags { get; }
    public int ExpirationInSeconds { get; }

    public RequestOutputCacheAttribute(string[] tags, int expirationInSeconds = RequestCacheConstants.ExpirationInSeconds)
    {
        Tags = tags;
        ExpirationInSeconds = expirationInSeconds;
    }
}