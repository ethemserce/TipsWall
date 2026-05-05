using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IUserDataService
    {
        Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(
            Guid userId, CancellationToken ct = default);

        Task<FavoriteOutcome> CreateFavoriteAsync(
            Guid userId, CreateFavoriteRequest request, CancellationToken ct = default);

        Task<bool> DeleteFavoriteAsync(
            Guid userId, Guid favoriteId, CancellationToken ct = default);

        Task<UserPreferencesDto> GetPreferencesAsync(
            Guid userId, CancellationToken ct = default);

        Task<PreferencesOutcome> UpsertPreferencesAsync(
            Guid userId, UpdateUserPreferencesRequest request, CancellationToken ct = default);
    }

    public sealed class FavoriteOutcome
    {
        public FavoriteDto? Favorite { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public bool Succeeded => Favorite != null && ErrorCode == null;

        public static FavoriteOutcome Ok(FavoriteDto fav) => new() { Favorite = fav };

        public static FavoriteOutcome Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };

        public static class ErrorCodes
        {
            public const string Validation = "VALIDATION";
            public const string Conflict = "CONFLICT";
        }
    }

    public sealed class PreferencesOutcome
    {
        public UserPreferencesDto? Preferences { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public bool Succeeded => Preferences != null && ErrorCode == null;

        public static PreferencesOutcome Ok(UserPreferencesDto p) => new() { Preferences = p };

        public static PreferencesOutcome Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };
    }
}
