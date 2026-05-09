namespace PreOddsApi.WebApi.V3.Contracts
{
    /// <summary>Initiates a password-reset flow. Always returns 200 even if the
    /// user doesn't exist — we don't want to leak account existence via timing
    /// or response shape. The actual email is sent (or queued) only when the
    /// account resolves.</summary>
    public sealed class ForgotPasswordRequest
    {
        public string EmailOrUsername { get; init; } = string.Empty;
    }

    /// <summary>Redeems a password-reset token issued via /auth/forgot-password.</summary>
    public sealed class ResetPasswordRequest
    {
        public string Token { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }

    /// <summary>Issues a fresh email-verification link to the authenticated user.</summary>
    public sealed class RequestEmailVerificationRequest
    {
        // No fields — user comes from the JWT.
    }

    /// <summary>Redeems an email-verification token.</summary>
    public sealed class VerifyEmailRequest
    {
        public string Token { get; init; } = string.Empty;
    }
}
