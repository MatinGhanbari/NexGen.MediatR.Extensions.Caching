namespace NexGen.MediatR.Extensions.Caching.Garnet.Containers;

/// <summary>
/// Reads and conditionally writes the serialized container index documents.
/// </summary>
internal interface IContainerIndexStore
{
    Task<string?> ReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the document only when the stored value still equals <paramref name="expected"/>.
    /// </summary>
    /// <param name="key">The index key.</param>
    /// <param name="expected">The document read before merging, or <c>null</c> when the key was absent.</param>
    /// <param name="updated">The merged document, or <c>null</c> to delete the key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the write was applied; <c>false</c> when a concurrent writer won.</returns>
    Task<bool> TryUpdateAsync(string key, string? expected, string? updated, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}

/// <summary>
/// The outcome of merging a request into an index document.
/// </summary>
internal readonly record struct ContainerIndexUpdate(bool Changed, string? Value)
{
    internal static ContainerIndexUpdate Unchanged { get; } = new(false, null);

    internal static ContainerIndexUpdate Delete { get; } = new(true, null);

    internal static ContainerIndexUpdate Write(string value) => new(true, value);
}
