using PreOddsApi.WebApi.V3.Auth;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Auth
{
    public class RefreshTokenGeneratorTests
    {
        [Fact]
        public void CreateRaw_returns_url_safe_string()
        {
            var token = RefreshTokenGenerator.CreateRaw();
            Assert.False(token.Contains('+'));
            Assert.False(token.Contains('/'));
            Assert.False(token.Contains('='));
            Assert.True(token.Length >= 32);
        }

        [Fact]
        public void CreateRaw_produces_different_values_each_call()
        {
            var first = RefreshTokenGenerator.CreateRaw();
            var second = RefreshTokenGenerator.CreateRaw();
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Hash_is_deterministic_for_same_input()
        {
            var raw = "fixed-input";
            var first = RefreshTokenGenerator.Hash(raw);
            var second = RefreshTokenGenerator.Hash(raw);
            Assert.Equal(first, second);
        }

        [Fact]
        public void Hash_returns_lowercase_hex()
        {
            var hash = RefreshTokenGenerator.Hash("anything");
            Assert.Equal(64, hash.Length);
            Assert.Equal(hash.ToLowerInvariant(), hash);
        }

        [Fact]
        public void Hash_produces_different_values_for_different_inputs()
        {
            var first = RefreshTokenGenerator.Hash("alpha");
            var second = RefreshTokenGenerator.Hash("beta");
            Assert.NotEqual(first, second);
        }
    }
}
