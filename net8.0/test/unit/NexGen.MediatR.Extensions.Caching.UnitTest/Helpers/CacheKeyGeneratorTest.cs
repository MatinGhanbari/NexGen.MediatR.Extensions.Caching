using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Helpers;

public class CacheKeyGeneratorTest
{
    private class TestRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public void GetCacheKey_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RequestOutputCacheHelper.GetCacheKey<TestRequest>(null));
    }

    [Theory]
    [InlineData(1, "Alice")]
    [InlineData(2, "Bob")]
    [InlineData(0, "")]
    public void GetCacheKey_WithRequest_ReturnsExpectedHash(int id, string name)
    {
        // Arrange
        var request = new TestRequest { Id = id, Name = name };
        var serialized = JsonSerializer.Serialize(request);

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(serialized));
        var expectedHash = BitConverter
            .ToString(hashBytes)
            .Replace("-", "")
            .ToLowerInvariant();

        var expectedCacheKey = $"{typeof(TestRequest).Name}:{expectedHash}";

        // Act
        var result = RequestOutputCacheHelper.GetCacheKey(request);

        // Assert
        Assert.Equal(expectedCacheKey, result);
    }

    [Fact]
    public void GetCacheKey_DifferentRequests_ProduceDifferentHashes()
    {
        // Arrange
        var request1 = new TestRequest { Id = 1, Name = "Alice" };
        var request2 = new TestRequest { Id = 2, Name = "Alice" };

        // Act
        var key1 = RequestOutputCacheHelper.GetCacheKey(request1);
        var key2 = RequestOutputCacheHelper.GetCacheKey(request2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GetCacheKey_SameRequest_ProducesSameHash()
    {
        // Arrange
        var request = new TestRequest { Id = 1, Name = "Alice" };

        // Act
        var key1 = RequestOutputCacheHelper.GetCacheKey(request);
        var key2 = RequestOutputCacheHelper.GetCacheKey(request);

        // Assert
        Assert.Equal(key1, key2);
    }
}