namespace NexGen.MediatR.Extensions.Caching.Messages;

/// <summary>
/// Payload published on Redis or Garnet Pub/Sub when cache tags are invalidated.
/// </summary>
public sealed class RequestOutputCacheEvictionNotification
{
    /// <summary>
    /// Cache tags to evict. For EF auto-evict these are typically entity CLR type names
    /// (for example <c>nameof(User)</c>).
    /// </summary>
    public string[] Tags { get; init; } = [];

    /// <summary>
    /// Identifier of the host that published this notification.
    /// Listeners ignore messages whose <see cref="SenderId"/> matches their own node.
    /// </summary>
    public string SenderId { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp in milliseconds since Unix epoch when the notification was created.
    /// </summary>
    public long TimestampUnixMs { get; init; }
}
