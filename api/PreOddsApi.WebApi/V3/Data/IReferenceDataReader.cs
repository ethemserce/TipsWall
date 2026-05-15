using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IReferenceDataReader
    {
        Task<(IReadOnlyList<CountryDto> Items, int Total)> GetCountriesAsync(
            long? continentId, string? search, string? iso2, int page, int perPage, CancellationToken ct = default);

        Task<CountryDto?> GetCountryByIdAsync(long id, CancellationToken ct = default);

        Task<IReadOnlyList<ContinentDto>> GetContinentsAsync(CancellationToken ct = default);

        Task<ContinentDto?> GetContinentByIdAsync(long id, CancellationToken ct = default);

        Task<(IReadOnlyList<LeagueDto> Items, int Total)> GetLeaguesAsync(
            long? countryId, bool? active, string? search, int page, int perPage, CancellationToken ct = default);

        Task<LeagueDto?> GetLeagueByIdAsync(long id, CancellationToken ct = default);

        Task<(IReadOnlyList<SeasonDto> Items, int Total)> GetSeasonsAsync(
            long? leagueId, bool? isCurrent, int page, int perPage, CancellationToken ct = default);

        Task<SeasonDto?> GetSeasonByIdAsync(long id, CancellationToken ct = default);

        Task<(IReadOnlyList<TeamDto> Items, int Total)> GetTeamsAsync(
            long? countryId, string? search, int page, int perPage, CancellationToken ct = default);

        Task<TeamDto?> GetTeamByIdAsync(long id, CancellationToken ct = default);

        /// <summary>
        /// Sezon istatistikleri — son hesaplanmış (en yeni as_of_date)
        /// season_team_stats satırlarını fixtureScope='all' için döner.
        /// Genelde bir takım birden fazla turnuvada oynuyor olabilir, o
        /// yüzden liste; mobile UI ilk satırı veya kullanıcının seçtiği
        /// ligi gösterir.
        /// </summary>
        Task<IReadOnlyList<TeamSeasonStatsDto>> GetTeamSeasonStatsAsync(
            long teamId, long? seasonId, CancellationToken ct = default);

        /// <summary>
        /// Takımın o sezondaki aktif kadrosu. seasonId null verilirse,
        /// team_squads içindeki en yeni season'ı kullanır (her takımın
        /// kayıtlı son sezonu).
        /// </summary>
        Task<IReadOnlyList<TeamSquadMemberDto>> GetTeamSquadAsync(
            long teamId, long? seasonId, CancellationToken ct = default);

        Task<(IReadOnlyList<BookmakerDto> Items, int Total)> GetBookmakersAsync(
            bool? active, int page, int perPage, CancellationToken ct = default);

        Task<(IReadOnlyList<MarketDto> Items, int Total)> GetMarketsAsync(
            bool? active, string? search, int page, int perPage, CancellationToken ct = default);
    }
}
