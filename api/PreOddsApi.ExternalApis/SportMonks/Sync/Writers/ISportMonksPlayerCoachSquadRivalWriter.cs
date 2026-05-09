using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksPlayerCoachSquadRivalWriter
    {
        Task UpsertPlayersAsync(
            IEnumerable<Player> players,
            CancellationToken cancellationToken = default);

        Task UpsertCoachesAsync(
            IEnumerable<Coach> coaches,
            CancellationToken cancellationToken = default);

        Task UpsertTeamSquadsAsync(
            IEnumerable<TeamSquad> teamSquads,
            CancellationToken cancellationToken = default);

        Task UpsertTeamRivalsAsync(
            IEnumerable<Rival> rivals,
            CancellationToken cancellationToken = default);
    }
}
