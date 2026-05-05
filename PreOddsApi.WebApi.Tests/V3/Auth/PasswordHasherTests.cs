using PreOddsApi.WebApi.V3.Auth;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Auth
{
    public class PasswordHasherTests
    {
        [Fact]
        public void Hash_then_Verify_returns_true_for_correct_password()
        {
            var hash = PasswordHasher.Hash("super-secret-password");
            Assert.True(PasswordHasher.Verify("super-secret-password", hash));
        }

        [Fact]
        public void Verify_returns_false_for_wrong_password()
        {
            var hash = PasswordHasher.Hash("super-secret-password");
            Assert.False(PasswordHasher.Verify("wrong-password", hash));
        }

        [Fact]
        public void Hash_produces_different_output_each_call_due_to_random_salt()
        {
            var first = PasswordHasher.Hash("same-password");
            var second = PasswordHasher.Hash("same-password");
            Assert.NotEqual(first, second);
            Assert.True(PasswordHasher.Verify("same-password", first));
            Assert.True(PasswordHasher.Verify("same-password", second));
        }

        [Fact]
        public void Hash_starts_with_algorithm_id()
        {
            var hash = PasswordHasher.Hash("anything");
            Assert.StartsWith(PasswordHasher.AlgorithmId + "$", hash);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("not-a-valid-hash")]
        [InlineData("wrong-algo$salt$hash")]
        [InlineData("pbkdf2-sha256-100000$invalid-base64$invalid-base64")]
        public void Verify_returns_false_for_invalid_or_missing_stored_value(string? storedValue)
        {
            Assert.False(PasswordHasher.Verify("any-password", storedValue!));
        }
    }
}
