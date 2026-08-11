using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Helpers
{
    public class CacheKeyGeneratorTest
    {
        private class TestRequest
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void GetCacheKey_WithNullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                RequestOutputCacheHelper.GetCacheKey<TestRequest>(null!));
        }

        [Theory]
        [InlineData(1, "Alice")]
        [InlineData(2, "Bob")]
        [InlineData(0, "")]
        public void GetCacheKey_WithRequest_ReturnsExpectedHash(int id, string name)
        {
            var request = new TestRequest { Id = id, Name = name };
            var serialized = JsonConvert.SerializeObject(request);

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(serialized));
            var expectedHash = BitConverter
                .ToString(hashBytes)
                .Replace("-", "")
                .ToLowerInvariant();

            var type = typeof(TestRequest);
            var fullName = type.FullName!;
            var typePath = fullName.Replace('.', ':').Replace('+', ':');
            var expectedCacheKey = $"{RequestCacheConstants.CacheKeyRootPrefix}:{typePath}:{expectedHash}";

            var result = RequestOutputCacheHelper.GetCacheKey(request);

            Assert.Equal(expectedCacheKey, result);
        }

        [Fact]
        public void GetCacheKey_StartsWithLibraryRootPrefix()
        {
            var request = new TestRequest { Id = 1, Name = "Alice" };

            var key = RequestOutputCacheHelper.GetCacheKey(request);

            Assert.StartsWith($"{RequestCacheConstants.CacheKeyRootPrefix}:", key);
        }

        [Fact]
        public void GetCacheKey_IncludesNamespaceAndTypeName()
        {
            var request = new TestRequest { Id = 1, Name = "Alice" };
            var type = typeof(TestRequest);
            var expectedSegment = type.FullName!.Replace('.', ':').Replace('+', ':');

            var key = RequestOutputCacheHelper.GetCacheKey(request);

            Assert.Contains(expectedSegment, key);
        }

        [Fact]
        public void GetCacheKey_SameShortName_DifferentNamespaces_ProduceDifferentKeys()
        {
            var a = new Collisions.Alpha.SameNameRequest { Value = 1 };
            var b = new Collisions.Beta.SameNameRequest { Value = 1 };

            var keyA = RequestOutputCacheHelper.GetCacheKey(a);
            var keyB = RequestOutputCacheHelper.GetCacheKey(b);

            Assert.NotEqual(keyA, keyB);
            Assert.Contains("Collisions:Alpha:SameNameRequest", keyA);
            Assert.Contains("Collisions:Beta:SameNameRequest", keyB);
        }

        [Fact]
        public void GetCacheKey_DifferentRequests_ProduceDifferentHashes()
        {
            var request1 = new TestRequest { Id = 1, Name = "Alice" };
            var request2 = new TestRequest { Id = 2, Name = "Alice" };

            var key1 = RequestOutputCacheHelper.GetCacheKey(request1);
            var key2 = RequestOutputCacheHelper.GetCacheKey(request2);

            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void GetCacheKey_SameRequest_ProducesSameHash()
        {
            var request = new TestRequest { Id = 1, Name = "Alice" };

            var key1 = RequestOutputCacheHelper.GetCacheKey(request);
            var key2 = RequestOutputCacheHelper.GetCacheKey(request);

            Assert.Equal(key1, key2);
        }
    }
}

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Helpers.Collisions.Alpha
{
    internal sealed class SameNameRequest
    {
        public int Value { get; set; }
    }
}

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Helpers.Collisions.Beta
{
    internal sealed class SameNameRequest
    {
        public int Value { get; set; }
    }
}
