namespace NexGen.MediatR.Extensions.Caching.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RequestOutputCacheAttribute : Attribute
{
    public string[] Tags { get; }
    public int ExpirationInSeconds { get; }

    public RequestOutputCacheAttribute(string[] tags, int expirationInSeconds = 0)
    {
        Tags = tags;
        ExpirationInSeconds = expirationInSeconds;
    }
}