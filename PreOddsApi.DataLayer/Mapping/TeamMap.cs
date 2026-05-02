using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
   public class TeamMap : BaseEntityMap<team>
    {
        public override void Configure(EntityTypeBuilder<team> builder)
        {
            builder.HasMany(x => x.aggregates)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);

            builder.HasMany(x => x.assistscorers)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);

                    builder.HasMany(x => x.benches)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);

                    builder.HasMany(x => x.cardscorers)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);

                    builder.HasMany(x => x.corners)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);

                    builder.HasMany(x => x.events)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);

                    builder.HasMany(x => x.localTeamfixtures)
        .WithOne(x => x.localTeam)
        .HasForeignKey(x => x.localTeamId);

                    builder.HasMany(x => x.visitorTeamfixtures)
        .WithOne(x => x.visitorTeam)
        .HasForeignKey(x => x.visitorTeamId);

                    builder.HasMany(x => x.formations)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);

                    builder.HasMany(x => x.goalscorers)
        .WithOne(x => x.team)
        .HasForeignKey(x => x.teamId);


            base.Configure(builder);
        }
    }
}
