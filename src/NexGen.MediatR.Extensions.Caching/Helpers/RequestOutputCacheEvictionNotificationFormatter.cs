using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.Helpers;

/// <summary>
/// Serializes and deserializes <see cref="RequestOutputCacheEvictionNotification"/> for Redis/Garnet Pub/Sub.
/// </summary>
internal static class RequestOutputCacheEvictionNotificationFormatter
{
    internal static string Serialize(RequestOutputCacheEvictionNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return JsonConvert.SerializeObject(notification);
    }

    internal static RequestOutputCacheEvictionNotification? TryDeserialize(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            var notification = JsonConvert.DeserializeObject<RequestOutputCacheEvictionNotification>(payload);
            if (notification is null)
                return null;

            var tags = RequestOutputCacheTagNormalizer.Normalize(notification.Tags);
            if (tags.Length == 0)
                return null;

            return new RequestOutputCacheEvictionNotification
            {
                Tags = tags,
                SenderId = notification.SenderId,
                TimestampUnixMs = notification.TimestampUnixMs
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
