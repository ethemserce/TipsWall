using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class UserDto
    {
        public Guid Id { get; init; }

        public string? Username { get; init; }

        public string? Email { get; init; }

        public string? DisplayName { get; init; }

        public string Role { get; init; } = "user";
    }
}
