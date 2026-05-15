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

        /// <summary>
        /// Membership tier — "free" or "premium". Drives feature gating
        /// across the mobile UI + server-side endpoints. Defaults to free
        /// at signup; flipped to premium by the IAP webhook.
        /// </summary>
        public string Tier { get; init; } = "free";

        /// <summary>
        /// When the current paid subscription expires. Null for free users.
        /// </summary>
        public DateTimeOffset? TierExpiresAt { get; init; }

        /// <summary>
        /// True when the user has clicked the email verification link.
        /// Sensitive operations (kupon kaydet, fav market değiştir) can
        /// soft-gate on this; mobile shows a banner until it flips.
        /// Social-signin accounts (Apple/Google) start true since the
        /// provider already verifies the email.
        /// </summary>
        public bool EmailVerified { get; init; }
    }
}
