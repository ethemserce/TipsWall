using AutoMapper;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Coupon;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.FixtureOfDay;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Tips;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.User;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.WebApi.Models;
using PreOddsApi.WebApi.Models.Bench.V2Models;
using PreOddsApi.WebApi.Models.bookmaker.V2Models;
using PreOddsApi.WebApi.Models.Coach.V2Models;
using PreOddsApi.WebApi.Models.Comment.V2Models;
using PreOddsApi.WebApi.Models.Continent;
using PreOddsApi.WebApi.Models.Continent.V2Models;
using PreOddsApi.WebApi.Models.Corner.V2Models;
using PreOddsApi.WebApi.Models.Country;
using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.Events.V2Models;
using PreOddsApi.WebApi.Models.Fixture;
using PreOddsApi.WebApi.Models.Fixture.V2Models;
using PreOddsApi.WebApi.Models.Fixture.V2Models.Analysis;
using PreOddsApi.WebApi.Models.FixtureDetail;
using PreOddsApi.WebApi.Models.FixtureOfDay.V2Models;
using PreOddsApi.WebApi.Models.Group.V2Models;
using PreOddsApi.WebApi.Models.Highlight.V2Models;
using PreOddsApi.WebApi.Models.League;
using PreOddsApi.WebApi.Models.League.V2Models;
using PreOddsApi.WebApi.Models.Lineup.V2Models;
using PreOddsApi.WebApi.Models.Market.V2Models;
using PreOddsApi.WebApi.Models.Odds.V2Models;
using PreOddsApi.WebApi.Models.Player.V2Models;
using PreOddsApi.WebApi.Models.Referee.V2Models;
using PreOddsApi.WebApi.Models.Round.V2Models;
using PreOddsApi.WebApi.Models.Season.V2Models;
using PreOddsApi.WebApi.Models.Sidelined.V2Models;
using PreOddsApi.WebApi.Models.Stage.V2Models;
using PreOddsApi.WebApi.Models.Standing.V2Models;
using PreOddsApi.WebApi.Models.Statistic.V2Models;
using PreOddsApi.WebApi.Models.Team;
using PreOddsApi.WebApi.Models.Team.V2Models;
using PreOddsApi.WebApi.Models.TopScorer.V2Models;
using PreOddsApi.WebApi.Models.TvStation.V2Models;
using PreOddsApi.WebApi.Models.Venue.V2Models;
using System;
using System.Collections.Generic;

namespace PreOddsApi.WebApi
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<LogoBusinessModel, LogoViewModel>();
            CreateMap<FixtureForRoundBusinessModel, LeagueInfoViewModel>();
            CreateMap<FixtureForDateBusinessModel, FixtureForDateViewModel>();
            CreateMap<FixtureForDateBaseBusinessModel, FixtureForDateBaseViewModel>();
            CreateMap<FixtureForLeagueBaseBusinessModel, FixtureForLeagueBaseViewModel>();
            CreateMap<FixtureForLeagueBaseBusinessModel, FixtureForLeagueBaseV2ViewModel>();
            CreateMap<FixtureForLiveBusinessModel, FixtureForLiveViewModel>();
            CreateMap<FixtureForLiveBusinessModel, FixtureForLiveV2ViewModel>();
            CreateMap<TopScorerBusinessModel, TopScorerViewModel>();
            CreateMap<TopScorerBusinessModel, TopScorerV2ViewModel>();
            CreateMap<OddAnalysisBaseBusinessModel, OddAnalysisBaseViewModel>();

            CreateMap<continent, ContinentBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time)).ReverseMap();

            //CreateMap<FixtureBusinessModel, ContinentViewModel>();

            CreateMap<country, CountryBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time)).ReverseMap();

            CreateMap<CountryBusinessModel, CountryViewModel>()
                 .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.ImagePath));
            CreateMap<CountryBusinessModel, CountryV2ViewModel>()
                      .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.ImagePath));

            CreateMap<league, LeagueBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.Cup, opt => opt.MapFrom(src => src.cup == 0 ? false : true))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.imagePath))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<LeagueBusinessModel, LeagueViewModel>();
            CreateMap<LeagueBusinessModel, LeagueV2ViewModel>();

            CreateMap<fixture, FixtureBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.EtScore, opt => opt.MapFrom(src => src.et_score))
            .ForMember(dest => dest.HtScore, opt => opt.MapFrom(src => src.localTeamHtScore + "-" + src.visitorTeamHtScore))
            .ForMember(dest => dest.FtScore, opt => opt.MapFrom(src => src.localTeamFtScore + "-" + src.visitorTeamFtScore))
            .ForMember(dest => dest.TimeStartingAtDate, opt => opt.MapFrom(src => src.startingAt.Value.Date.ToString()))
            .ForMember(dest => dest.TimeStartingAtDateTime, opt => opt.MapFrom(src => src.startingAt.Value.ToString()))
            .ForMember(dest => dest.TimeStartingAtTime, opt => opt.MapFrom(src => src.startingAt.Value.ToShortTimeString()))
            .ForMember(dest => dest.TimeStartingAtTimestamp, opt => opt.MapFrom(src => src.startingAtTimestamp))
            .ForMember(dest => dest.TimeStartingAtTimezone, opt => opt.MapFrom(src => src.startingAtTimestamp))
            .ForMember(dest => dest.TimeStatus, opt => opt.MapFrom(src => src.status))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time))
            .ForMember(dest => dest.LocalTeamScore, opt => opt.MapFrom(src => src.localTeamFtScore))
            .ForMember(dest => dest.VisitorTeamScore, opt => opt.MapFrom(src => src.visitorTeamFtScore));
            //.ForMember(dest => dest.WinningOddsCalculated, opt => opt.MapFrom(src => src.winning_odds_calculated == null ? false : (src.winning_odds_calculated == 0 ? false : true)));

            
            
            CreateMap<fixture, FixtureDetailHeaderBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.EtScore, opt => opt.MapFrom(src => src.et_score))
            .ForMember(dest => dest.HtScore, opt => opt.MapFrom(src => src.localTeamHtScore + "-" + src.visitorTeamHtScore))
            .ForMember(dest => dest.FtScore, opt => opt.MapFrom(src => src.localTeamFtScore + "-" + src.visitorTeamFtScore))
            //.ForMember(dest => dest.LocalTeamCoachId, opt => opt.MapFrom(src => src.local_team_coach_id))
            //.ForMember(dest => dest.LocalTeamPenScore, opt => opt.MapFrom(src => src.local_team_pen_score))
            .ForMember(dest => dest.LocalTeamScore, opt => opt.MapFrom(src => src.localTeamFtScore))
            //.ForMember(dest => dest.TimeAddedTime, opt => opt.MapFrom(src => src.time_added_time))
            //.ForMember(dest => dest.TimeExtraMinute, opt => opt.MapFrom(src => src.time_extra_minute))
            //.ForMember(dest => dest.TimeInjuryTime, opt => opt.MapFrom(src => src.time_injury_time))
            //.ForMember(dest => dest.TimeMinute, opt => opt.MapFrom(src => src.time_minute))
            .ForMember(dest => dest.TimeStartingAtDate, opt => opt.MapFrom(src => src.startingAt.Value.Date))
            .ForMember(dest => dest.TimeStartingAtDateTime, opt => opt.MapFrom(src => src.startingAt))
            .ForMember(dest => dest.TimeStartingAtTime, opt => opt.MapFrom(src => src.startingAt.Value.ToShortTimeString()))
            .ForMember(dest => dest.TimeStartingAtTimestamp, opt => opt.MapFrom(src => src.startingAtTimestamp))
            //.ForMember(dest => dest.TimeStartingAtTimezone, opt => opt.MapFrom(src => src.time_starting_at_timezone))
            //.ForMember(dest => dest.TimeStatus, opt => opt.MapFrom(src => src.time_status))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time))
            //.ForMember(dest => dest.VisitorTeamCoachId, opt => opt.MapFrom(src => src.visitor_team_coach_id))
            //.ForMember(dest => dest.VisitorTeamPenScore, opt => opt.MapFrom(src => src.visitor_team_pen_score))
            .ForMember(dest => dest.VisitorTeamScore, opt => opt.MapFrom(src => src.visitorTeamFtScore));
            CreateMap<FixtureDetailHeaderBusinessModel, FixtureDetailHeaderViewModel>();

            CreateMap<fixture, FixtureForLeagueBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.EtScore, opt => opt.MapFrom(src => src.et_score))
            .ForMember(dest => dest.HtScore, opt => opt.MapFrom(src => src.localTeamHtScore + "-" + src.visitorTeamHtScore))
            .ForMember(dest => dest.FtScore, opt => opt.MapFrom(src => src.localTeamFtScore + "-" + src.visitorTeamFtScore))
            .ForMember(dest => dest.LocalTeamScore, opt => opt.MapFrom(src => src.localTeamFtScore))
            //.ForMember(dest => dest.LocalTeamPenScore, opt => opt.MapFrom(src => src.local_team_pen_score))
            .ForMember(dest => dest.VisitorTeamScore, opt => opt.MapFrom(src => src.visitorTeamFtScore))
            //.ForMember(dest => dest.VisitorTeamPenScore, opt => opt.MapFrom(src => src.visitor_team_pen_score))
            //.ForMember(dest => dest.TimeAddedTime, opt => opt.MapFrom(src => src.time_added_time))
            //.ForMember(dest => dest.TimeExtraMinute, opt => opt.MapFrom(src => src.time_extra_minute))
            //.ForMember(dest => dest.TimeInjuryTime, opt => opt.MapFrom(src => src.time_injury_time))
            //.ForMember(dest => dest.TimeMinute, opt => opt.MapFrom(src => src.time_minute))
            .ForMember(dest => dest.TimeStartingAtDate, opt => opt.MapFrom(src => src.startingAt.Value.Date))
            .ForMember(dest => dest.TimeStartingAtDateTime, opt => opt.MapFrom(src => src.startingAt))
            .ForMember(dest => dest.TimeStartingAtTime, opt => opt.MapFrom(src => src.startingAt.Value.ToShortTimeString()))
            .ForMember(dest => dest.TimeStartingAtTimestamp, opt => opt.MapFrom(src => src.startingAtTimestamp))
            //.ForMember(dest => dest.TimeStartingAtTimezone, opt => opt.MapFrom(src => src.time_starting_at_timezone))
            .ForMember(dest => dest.TimeStatus, opt => opt.MapFrom(src => src.status))
            //.ForMember(dest => dest.IddaaCode, opt => opt.MapFrom(src => src.iddaa_code))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<FixtureForLeagueBusinessModel, FixtureForLeagueViewModel>();
            CreateMap<FixtureForLeagueBusinessModel, FixtureForLeagueV2ViewModel>();

            CreateMap<fixture, FixtureForLastMatchesBusinessModel>()
             .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.EtScore, opt => opt.MapFrom(src => src.et_score))
            .ForMember(dest => dest.HtScore, opt => opt.MapFrom(src => src.localTeamHtScore + "-" + src.visitorTeamHtScore))
            .ForMember(dest => dest.FtScore, opt => opt.MapFrom(src => src.localTeamFtScore + "-" + src.visitorTeamFtScore))
            //.ForMember(dest => dest.LocalTeamPenScore, opt => opt.MapFrom(src => src.local_team_pen_score))
            .ForMember(dest => dest.LocalTeamScore, opt => opt.MapFrom(src => src.localTeamFtScore))            // .ForMember(dest => dest.TimeAddedTime, opt => opt.MapFrom(src => src.time_added_time))
            // .ForMember(dest => dest.TimeExtraMinute, opt => opt.MapFrom(src => src.time_extra_minute))
            // .ForMember(dest => dest.TimeInjuryTime, opt => opt.MapFrom(src => src.time_injury_time))
            // .ForMember(dest => dest.TimeMinute, opt => opt.MapFrom(src => src.time_minute))
            .ForMember(dest => dest.TimeStartingAtDate, opt => opt.MapFrom(src => src.startingAt.Value.Date))
            .ForMember(dest => dest.TimeStartingAtDateTime, opt => opt.MapFrom(src => src.startingAt))
            .ForMember(dest => dest.TimeStartingAtTime, opt => opt.MapFrom(src => src.startingAt.Value.ToShortTimeString()))
            .ForMember(dest => dest.TimeStartingAtTimestamp, opt => opt.MapFrom(src => src.startingAtTimestamp))
              // .ForMember(dest => dest.TimeStartingAtTimezone, opt => opt.MapFrom(src => src.time_starting_at_timezone))
              .ForMember(dest => dest.TimeStatus, opt => opt.MapFrom(src => src.status))
             //.ForMember(dest => dest.VisitorTeamPenScore, opt => opt.MapFrom(src => src.visitor_team_pen_score))
             .ForMember(dest => dest.VisitorTeamScore, opt => opt.MapFrom(src => src.visitorTeamFtScore))
             //.ForMember(dest => dest.IddaaCode, opt => opt.MapFrom(src => src.iddaa_code))
             .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<FixtureForLastMatchesBusinessModel, LastMatchesViewModel>();

            CreateMap<fixture, FixtureForDateBusinessModel>()
         .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            .ForMember(dest => dest.HtScore, opt => opt.MapFrom(src => src.localTeamHtScore + "-" + src.visitorTeamHtScore))
            .ForMember(dest => dest.FtScore, opt => opt.MapFrom(src => src.localTeamFtScore + "-" + src.visitorTeamFtScore))
            //.ForMember(dest => dest.EtScore, opt => opt.MapFrom(src => src.ht_score))
            //.ForMember(dest => dest.LocalTeamPenScore, opt => opt.MapFrom(src => src.local_team_pen_score))
            .ForMember(dest => dest.LocalTeamScore, opt => opt.MapFrom(src => src.localTeamFtScore))
            // .ForMember(dest => dest.TimeAddedTime, opt => opt.MapFrom(src => src.time_added_time))
            // .ForMember(dest => dest.TimeExtraMinute, opt => opt.MapFrom(src => src.time_extra_minute))
            // .ForMember(dest => dest.TimeInjuryTime, opt => opt.MapFrom(src => src.time_injury_time))
            // .ForMember(dest => dest.TimeMinute, opt => opt.MapFrom(src => src.time_minute))
            .ForMember(dest => dest.TimeStartingAtDate, opt => opt.MapFrom(src => src.startingAt.Value.Date))
            .ForMember(dest => dest.TimeStartingAtDateTime, opt => opt.MapFrom(src => src.startingAt))
            .ForMember(dest => dest.TimeStartingAtTime, opt => opt.MapFrom(src => src.startingAt.Value.ToShortTimeString()))
            .ForMember(dest => dest.TimeStartingAtTimestamp, opt => opt.MapFrom(src => src.startingAtTimestamp))
              // .ForMember(dest => dest.TimeStartingAtTimezone, opt => opt.MapFrom(src => src.time_starting_at_timezone))
              .ForMember(dest => dest.TimeStatus, opt => opt.MapFrom(src => src.status))
             //.ForMember(dest => dest.VisitorTeamPenScore, opt => opt.MapFrom(src => src.visitor_team_pen_score))
             .ForMember(dest => dest.VisitorTeamScore, opt => opt.MapFrom(src => src.visitorTeamFtScore))
         //.ForMember(dest => dest.IddaaCode, opt => opt.MapFrom(src => src.iddaa_code))
         .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<FixtureForDateBusinessModel, FixtureForDateViewModel>();

            CreateMap<fixture, FixtureForOddAnalysisBusinessModel>()
             .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.EtScore, opt => opt.MapFrom(src => src.et_score))
            .ForMember(dest => dest.HtScore, opt => opt.MapFrom(src => src.localTeamHtScore + "-" + src.visitorTeamHtScore))
            .ForMember(dest => dest.FtScore, opt => opt.MapFrom(src => src.localTeamFtScore + "-" + src.visitorTeamFtScore))
              //.ForMember(dest => dest.LocalTeamPenScore, opt => opt.MapFrom(src => src.local_team_pen_score))
              .ForMember(dest => dest.LocalTeamScore, opt => opt.MapFrom(src => src.localTeamFtScore))
              // .ForMember(dest => dest.TimeAddedTime, opt => opt.MapFrom(src => src.time_added_time))
              // .ForMember(dest => dest.TimeExtraMinute, opt => opt.MapFrom(src => src.time_extra_minute))
              // .ForMember(dest => dest.TimeInjuryTime, opt => opt.MapFrom(src => src.time_injury_time))
              // .ForMember(dest => dest.TimeMinute, opt => opt.MapFrom(src => src.time_minute))
              .ForMember(dest => dest.TimeStartingAtDate, opt => opt.MapFrom(src => src.startingAt.Value.Date))
              .ForMember(dest => dest.TimeStartingAtDateTime, opt => opt.MapFrom(src => src.startingAt))
              .ForMember(dest => dest.TimeStartingAtTime, opt => opt.MapFrom(src => src.startingAt.Value.Date.ToShortTimeString()))
              .ForMember(dest => dest.TimeStartingAtTimestamp, opt => opt.MapFrom(src => src.startingAtTimestamp))
              // .ForMember(dest => dest.TimeStartingAtTimezone, opt => opt.MapFrom(src => src.time_starting_at_timezone))
              .ForMember(dest => dest.TimeStatus, opt => opt.MapFrom(src => src.status))
             //.ForMember(dest => dest.VisitorTeamPenScore, opt => opt.MapFrom(src => src.visitor_team_pen_score))
             .ForMember(dest => dest.VisitorTeamScore, opt => opt.MapFrom(src => src.visitorTeamFtScore))
             //.ForMember(dest => dest.IddaaCode, opt => opt.MapFrom(src => src.iddaa_code))
             .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));

            CreateMap<FixtureBusinessModel, FixtureV2ViewModel>().ReverseMap();

            CreateMap<FixtureForOddAnalysisBusinessModel, FixtureForOddAnalysisViewModel>().ReverseMap();
            CreateMap<FixtureForOddAnalysisBusinessModel, FixtureForOddAnalysisV2ViewModel>().ReverseMap();
            CreateMap<OddBusinessModel, OddV2ViewModel>().ReverseMap();
            CreateMap<MarketBusinessModel, MarketV2ViewModel>().ReverseMap();
            CreateMap<BookmakerBusinessModel, bookmakerV2ViewModel>().ReverseMap();
            CreateMap<OddAnalysisBusinessModel, OddAnalysisV2ViewModel>().ReverseMap();
            CreateMap<BookmakerBusinessModel, bookmaker>().ReverseMap();
            CreateMap<BookmakerBusinessModel, bookmakerViewModel>().ReverseMap();
            CreateMap<OddAnalysisBusinessModel, OddAnalysisViewModel>().ReverseMap();
            CreateMap<MarketViewModel, MarketForOddsV2ViewModel>().ReverseMap();
            CreateMap<bookmakerViewModel, bookmakerForOddsV2ViewModel>().ReverseMap();
            CreateMap<OddAnalysisViewModel, OddAnalysisV2ViewModel>().ReverseMap();
            CreateMap<LineupBusinessModel, BenchViewModel>()
                .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.JerseyNumber)).ReverseMap();
            CreateMap<LineupBusinessModel, BenchV2ViewModel>()
                 .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.JerseyNumber)).ReverseMap();
            CreateMap<StatisticAnalysisBusinessModel, StatisticAnalysisViewModel>().ReverseMap();
            CreateMap<TeamBusinessModel, TeamForFixtureViewModel>().ReverseMap();
            CreateMap<FixtureViewModel, FixtureV2ViewModel>().ReverseMap();
            CreateMap<LeagueViewModel, LeagueV2ViewModel>().ReverseMap();
            CreateMap<CountryViewModel, CountryV2ViewModel>().ReverseMap();
            CreateMap<BenchViewModel, BenchV2ViewModel>().ReverseMap();
            CreateMap<PlayerViewModel, PlayerV2ViewModel>().ReverseMap();

            CreateMap<StatisticAnalysisViewModel, StatisticAnalysisV2ViewModel>().ReverseMap();
            CreateMap<FixtureForLeagueViewModel, FixtureForLeagueV2ViewModel>().ReverseMap();
            CreateMap<TeamForFixtureViewModel, TeamV2ViewModel>().ReverseMap();
            CreateMap<TeamViewModel, TeamV2ViewModel>().ReverseMap();
            CreateMap<RoundViewModel, RoundV2ViewModel>().ReverseMap();
            CreateMap<SeasonViewModel, SeasonV2ViewModel>().ReverseMap();
            CreateMap<StageViewModel, StageV2ViewModel>().ReverseMap();
            CreateMap<VenueViewModel, VenueV2ViewModel>().ReverseMap();
            CreateMap<CoachViewModel, CoachV2ViewModel>().ReverseMap();
            CreateMap<CornerViewModel, CornerV2ViewModel>().ReverseMap();
            CreateMap<SidelinedViewModel, SidelinedV2ViewModel>().ReverseMap();
            CreateMap<StatisticViewModel, StatisticV2ViewModel>().ReverseMap();
            CreateMap<EventsViewModel, EventsV2ViewModel>().ReverseMap();
            CreateMap<CommentViewModel, CommentV2ViewModel>().ReverseMap();
            CreateMap<HighlightViewModel, HighlightV2ViewModel>().ReverseMap();
            CreateMap<TvstationViewModel, TvstationV2ViewModel>().ReverseMap();
            CreateMap<ContinentViewModel, ContinentV2ViewModel>().ReverseMap();
            CreateMap<FixtureBusinessModel, FixtureViewModel>().ReverseMap();
            CreateMap<LeagueViewModel, LeagueDetailV2ViewModel>().ReverseMap();
            CreateMap<GroupBusinessModel, GroupV2ViewModel>().ReverseMap();
            CreateMap<StatisticBusinessModel, StatisticV2ViewModel>().ReverseMap();
            CreateMap<StandingBusinessModel, StandingV2ViewModel>().ReverseMap();

            CreateMap<FixtureBusinessModel, FixtureForFixtureOfDayBusinessModel>().ReverseMap();
            CreateMap<FixtureOfDayBusinessModel, FixtureOfDayV2ViewModel>().ReverseMap();
            CreateMap<FixtureForFixtureOfDayBusinessModel, FixtureForFixtureOfDayV2ViewModel>().ReverseMap();
            CreateMap<OddForFixtureOfDayBusinessModel, OddForFixtureOfDayV2ViewModel>().ReverseMap();
            CreateMap<FixtureForOddAnalysisBaseBusinessModel, FixtureForOddAnalysisBaseV2ViewModel>().ReverseMap();


            CreateMap<group, GroupBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.RoundId, opt => opt.MapFrom(src => src.round_id))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<GroupBusinessModel, GroupViewModel>();

            CreateMap<round, RoundBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.End, opt => opt.MapFrom(src => src.end))
            //.ForMember(dest => dest.Start, opt => opt.MapFrom(src => src.start))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<RoundBusinessModel, RoundViewModel>();
            CreateMap<RoundBusinessModel, RoundV2ViewModel>();

            CreateMap<coach, CoachBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.BirthCountry, opt => opt.MapFrom(src => src.birth_country))
            //.ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.birth_date))
            //.ForMember(dest => dest.BirthPlace, opt => opt.MapFrom(src => src.birth_place))
            //.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.full_name))
            //.ForMember(dest => dest.Nationality, opt => opt.MapFrom(src => src.nationality))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<CoachBusinessModel, CoachViewModel>();
            CreateMap<CoachBusinessModel, CoachV2ViewModel>();

            CreateMap<stage, StageBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.type))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<StageBusinessModel, StageViewModel>();
            CreateMap<StageBusinessModel, StageV2ViewModel>();


            CreateMap<season, SeasonBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.CurrentRoundId, opt => opt.MapFrom(src => src.current_round_id))
            //.ForMember(dest => dest.CurrentStageId, opt => opt.MapFrom(src => src.current_stage_id))
            //.ForMember(dest => dest.CurrentSeason, opt => opt.MapFrom(src => src.current_season == 0 ? false : true))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<SeasonBusinessModel, SeasonViewModel>();
            CreateMap<SeasonBusinessModel, SeasonV2ViewModel>();

            CreateMap<team, TeamBusinessModel>()
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.LegacyId, opt => opt.MapFrom(src => src.legacy_id))
            //.ForMember(dest => dest.NationalTeam, opt => opt.MapFrom(src => src.national_team == 0 ? false : true))
            //.ForMember(dest => dest.Twitter, opt => opt.MapFrom(src => src.twitter))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time)).ReverseMap(); 
            CreateMap<TeamBusinessModel, TeamViewModel>().ReverseMap(); 
            CreateMap<TeamBusinessModel, TeamV2ViewModel>().ReverseMap();

            CreateMap<standing, StandingBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.StandingsAwayDraw, opt => opt.MapFrom(src => src.standings_away_draw))
            //.ForMember(dest => dest.StandingsAwayGamesPlayed, opt => opt.MapFrom(src => src.standings_away_games_played))
            //.ForMember(dest => dest.StandingsAwayGoalsAgainst, opt => opt.MapFrom(src => src.standings_away_goals_against))
            //.ForMember(dest => dest.StandingsAwayGoalsScored, opt => opt.MapFrom(src => src.standings_away_goals_scored))
            //.ForMember(dest => dest.StandingsAwayLost, opt => opt.MapFrom(src => src.standings_away_lost))
            //.ForMember(dest => dest.StandingsAwayWon, opt => opt.MapFrom(src => src.standings_away_won))
            //.ForMember(dest => dest.StandingsGroupId, opt => opt.MapFrom(src => src.standings_group_id))
            //.ForMember(dest => dest.StandingsHomeDraw, opt => opt.MapFrom(src => src.standings_home_draw))
            //.ForMember(dest => dest.StandingsHomeGamesPlayed, opt => opt.MapFrom(src => src.standings_home_games_played))
            //.ForMember(dest => dest.StandingsHomeGoalsAgainst, opt => opt.MapFrom(src => src.standings_home_goals_against))
            //.ForMember(dest => dest.StandingsHomeGoalsScored, opt => opt.MapFrom(src => src.standings_home_goals_scored))
            //.ForMember(dest => dest.StandingsHomeLost, opt => opt.MapFrom(src => src.standings_home_lost))
            //.ForMember(dest => dest.StandingsHomeWon, opt => opt.MapFrom(src => src.standings_home_won))
            //.ForMember(dest => dest.StandingsOverallDraw, opt => opt.MapFrom(src => src.standings_overall_draw))
            //.ForMember(dest => dest.StandingsOverallGamesPlayed, opt => opt.MapFrom(src => src.standings_overall_games_played))
            //.ForMember(dest => dest.StandingsOverallGoalsAgainst, opt => opt.MapFrom(src => src.standings_overall_goals_against))
            //.ForMember(dest => dest.StandingsOverallGoalsScored, opt => opt.MapFrom(src => src.standings_overall_goals_scored))
            //.ForMember(dest => dest.StandingsOverallLost, opt => opt.MapFrom(src => src.standings_overall_lost))
            //.ForMember(dest => dest.StandingsOverallWon, opt => opt.MapFrom(src => src.standings_overall_won))
            //.ForMember(dest => dest.StandingsPositon, opt => opt.MapFrom(src => src.standings_positon))
            //.ForMember(dest => dest.StandingsRecentForm, opt => opt.MapFrom(src => src.standings_recent_form))
            .ForMember(dest => dest.StandingsTeamId, opt => opt.MapFrom(src => src.teamId))
            //.ForMember(dest => dest.StandingsTotalGoalDifference, opt => opt.MapFrom(src => src.standings_total_goal_difference))
            //.ForMember(dest => dest.StandingsTotalPoints, opt => opt.MapFrom(src => src.standings_total_points))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<StandingBusinessModel, StandingViewModel>();

            CreateMap<comment, CommentBusinessModel>()
             .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
             .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<CommentBusinessModel, CommentViewModel>();
            CreateMap<CommentBusinessModel, CommentV2ViewModel>();

            CreateMap<corner, CornerBusinessModel>()
              .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
              .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<CornerBusinessModel, CornerViewModel>();
            CreateMap<CornerBusinessModel, CornerV2ViewModel>();

            CreateMap<highlight, HighlightBusinessModel>()
           .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
           .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<HighlightBusinessModel, HighlightViewModel>();
            CreateMap<HighlightBusinessModel, HighlightV2ViewModel>();

            CreateMap<LineupViewModel, LineupV2ViewModel>().ReverseMap();
            CreateMap<LineupBusinessModel, LineupViewModel>();
            CreateMap<LineupBusinessModel, LineupV2ViewModel>()
                    .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.JerseyNumber));
            CreateMap<lineup, LineupBusinessModel>()
             .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            //.ForMember(dest => dest.AdditionalPosition, opt => opt.MapFrom(src => src.additional_position))
            // .ForMember(dest => dest.Posx, opt => opt.MapFrom(src => src.posx))
            // .ForMember(dest => dest.Posy, opt => opt.MapFrom(src => src.posy))
            //.ForMember(dest => dest.StatsCardsRedcards, opt => opt.MapFrom(src => src.stats_cards_redcards))
            // .ForMember(dest => dest.StatsCardsYellowcards, opt => opt.MapFrom(src => src.stats_cards_yellowcards))
            // .ForMember(dest => dest.StatsFoulsCommitted, opt => opt.MapFrom(src => src.stats_fouls_committed))
            //.ForMember(dest => dest.StatsFoulsDrawn, opt => opt.MapFrom(src => src.stats_fouls_drawn))
            // .ForMember(dest => dest.StatsGoalsConceded, opt => opt.MapFrom(src => src.stats_goals_conceded))
            // .ForMember(dest => dest.StatsGoalsScored, opt => opt.MapFrom(src => src.stats_goals_scored))
            //.ForMember(dest => dest.StatsOtherAssists, opt => opt.MapFrom(src => src.stats_other_assists))
            // .ForMember(dest => dest.StatsOtherBlocks, opt => opt.MapFrom(src => src.stats_other_blocks))
            // .ForMember(dest => dest.StatsOtherClearances, opt => opt.MapFrom(src => src.stats_other_clearances))
            //.ForMember(dest => dest.StatsOtherHitWoodwork, opt => opt.MapFrom(src => src.stats_other_hit_woodwork))
            // .ForMember(dest => dest.StatsOtherInterceptions, opt => opt.MapFrom(src => src.stats_other_interceptions))
            // .ForMember(dest => dest.StatsOtherMinutesPlayed, opt => opt.MapFrom(src => src.stats_other_minutes_played))
            //.ForMember(dest => dest.StatsOtherOffsides, opt => opt.MapFrom(src => src.stats_other_offsides))
            // .ForMember(dest => dest.StatsOtherPenCommitted, opt => opt.MapFrom(src => src.stats_other_pen_committed))
            // .ForMember(dest => dest.StatsOtherPenMissed, opt => opt.MapFrom(src => src.stats_other_pen_missed))
            //.ForMember(dest => dest.StatsOtherPenSaved, opt => opt.MapFrom(src => src.stats_other_pen_saved))
            // .ForMember(dest => dest.StatsOtherPenScored, opt => opt.MapFrom(src => src.stats_other_pen_scored))
            // .ForMember(dest => dest.StatsOtherPenWon, opt => opt.MapFrom(src => src.stats_other_pen_won))
            //.ForMember(dest => dest.StatsOtherSaves, opt => opt.MapFrom(src => src.stats_other_saves))
            // .ForMember(dest => dest.StatsOtherTackles, opt => opt.MapFrom(src => src.stats_other_tackles))
            // .ForMember(dest => dest.StatsPassingCrossesAccuracy, opt => opt.MapFrom(src => src.stats_passing_crosses_accuracy))
            //.ForMember(dest => dest.StatsPassingPasses, opt => opt.MapFrom(src => src.stats_passing_passes))
            // .ForMember(dest => dest.StatsPassingPassesAccuracy, opt => opt.MapFrom(src => src.stats_passing_passes_accuracy))
            // .ForMember(dest => dest.StatsPassingTotalCrosses, opt => opt.MapFrom(src => src.stats_passing_total_crosses))
            //.ForMember(dest => dest.StatsShotsShotsOnGoal, opt => opt.MapFrom(src => src.stats_shots_shots_on_goal))
            // .ForMember(dest => dest.StatsShotsShotsTotal, opt => opt.MapFrom(src => src.stats_shots_shots_total))



            CreateMap<bench, BenchBusinessModel>()
          .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
          .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
          .ForMember(dest => dest.AdditionalPosition, opt => opt.MapFrom(src => src.additional_position))
         .ForMember(dest => dest.FormationPosition, opt => opt.MapFrom(src => src.formation_position))
          .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.number))
            .ForMember(dest => dest.PlayerName, opt => opt.MapFrom(src => src.player_name))
         .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.position))
          .ForMember(dest => dest.Posx, opt => opt.MapFrom(src => src.posx))
          .ForMember(dest => dest.Posy, opt => opt.MapFrom(src => src.posy))
         .ForMember(dest => dest.StatsCardsRedcards, opt => opt.MapFrom(src => src.stats_cards_redcards))
          .ForMember(dest => dest.StatsCardsYellowcards, opt => opt.MapFrom(src => src.stats_cards_yellowcards))
          .ForMember(dest => dest.StatsFoulsCommitted, opt => opt.MapFrom(src => src.stats_fouls_committed))
         .ForMember(dest => dest.StatsFoulsDrawn, opt => opt.MapFrom(src => src.stats_fouls_drawn))
          .ForMember(dest => dest.StatsGoalsConceded, opt => opt.MapFrom(src => src.stats_goals_conceded))
          .ForMember(dest => dest.StatsGoalsScored, opt => opt.MapFrom(src => src.stats_goals_scored))
         .ForMember(dest => dest.StatsOtherAssists, opt => opt.MapFrom(src => src.stats_other_assists))
          .ForMember(dest => dest.StatsOtherBlocks, opt => opt.MapFrom(src => src.stats_other_blocks))
          .ForMember(dest => dest.StatsOtherClearances, opt => opt.MapFrom(src => src.stats_other_clearances))
         .ForMember(dest => dest.StatsOtherHitWoodwork, opt => opt.MapFrom(src => src.stats_other_hit_woodwork))
          .ForMember(dest => dest.StatsOtherInterceptions, opt => opt.MapFrom(src => src.stats_other_interceptions))
          .ForMember(dest => dest.StatsOtherMinutesPlayed, opt => opt.MapFrom(src => src.stats_other_minutes_played))
         .ForMember(dest => dest.StatsOtherOffsides, opt => opt.MapFrom(src => src.stats_other_offsides))
          .ForMember(dest => dest.StatsOtherPenCommitted, opt => opt.MapFrom(src => src.stats_other_pen_committed))
          .ForMember(dest => dest.StatsOtherPenMissed, opt => opt.MapFrom(src => src.stats_other_pen_missed))
         .ForMember(dest => dest.StatsOtherPenSaved, opt => opt.MapFrom(src => src.stats_other_pen_saved))
          .ForMember(dest => dest.StatsOtherPenScored, opt => opt.MapFrom(src => src.stats_other_pen_scored))
          .ForMember(dest => dest.StatsOtherPenWon, opt => opt.MapFrom(src => src.stats_other_pen_won))
         .ForMember(dest => dest.StatsOtherSaves, opt => opt.MapFrom(src => src.stats_other_saves))
          .ForMember(dest => dest.StatsOtherTackles, opt => opt.MapFrom(src => src.stats_other_tackles))
          .ForMember(dest => dest.StatsPassingCrossesAccuracy, opt => opt.MapFrom(src => src.stats_passing_crosses_accuracy))
         .ForMember(dest => dest.StatsPassingPasses, opt => opt.MapFrom(src => src.stats_passing_passes))
          .ForMember(dest => dest.StatsPassingPassesAccuracy, opt => opt.MapFrom(src => src.stats_passing_passes_accuracy))
          .ForMember(dest => dest.StatsPassingTotalCrosses, opt => opt.MapFrom(src => src.stats_passing_total_crosses))
         .ForMember(dest => dest.StatsShotsShotsOnGoal, opt => opt.MapFrom(src => src.stats_shots_shots_on_goal))
          .ForMember(dest => dest.StatsShotsShotsTotal, opt => opt.MapFrom(src => src.stats_shots_shots_total))
          .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<BenchBusinessModel, BenchViewModel>();
            CreateMap<BenchBusinessModel, BenchV2ViewModel>();


            CreateMap<player, PlayerBusinessModel>()
              .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
              .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
              //.ForMember(dest => dest.BirthCountry, opt => opt.MapFrom(src => src.birth_country))
              //.ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.birth_date))
              //.ForMember(dest => dest.BirthPlace, opt => opt.MapFrom(src => src.birth_place))
              //.ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.first_name))
              //  .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.full_name))
              //.ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.last_name))
              //  .ForMember(dest => dest.Nationality, opt => opt.MapFrom(src => src.nationality))
              .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.weight))
              .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<PlayerBusinessModel, PlayerViewModel>();
            CreateMap<PlayerBusinessModel, PlayerV2ViewModel>();

            CreateMap<referee, RefereeBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            //.ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.first_name))
            //.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.full_name))
            //.ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.last_name))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<RefereeBusinessModel, RefereeViewModel>();
            CreateMap<RefereeBusinessModel, RefereeV2ViewModel>();

            CreateMap<sidelined, SidelinedBusinessModel>()
             .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
             .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            // .ForMember(dest => dest.AdditionalPosition, opt => opt.MapFrom(src => src.additional_position))
            //.ForMember(dest => dest.FormationPosition, opt => opt.MapFrom(src => src.formation_position))
            // .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.number))
            //.ForMember(dest => dest.PlayerName, opt => opt.MapFrom(src => src.player_name))
            //.ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.position))
            // .ForMember(dest => dest.Posx, opt => opt.MapFrom(src => src.posx))
            // .ForMember(dest => dest.Posy, opt => opt.MapFrom(src => src.posy))
            //.ForMember(dest => dest.StatsCardsRedcards, opt => opt.MapFrom(src => src.stats_cards_redcards))
            // .ForMember(dest => dest.StatsCardsYellowcards, opt => opt.MapFrom(src => src.stats_cards_yellowcards))
            // .ForMember(dest => dest.StatsFoulsCommitted, opt => opt.MapFrom(src => src.stats_fouls_committed))
            //.ForMember(dest => dest.StatsFoulsDrawn, opt => opt.MapFrom(src => src.stats_fouls_drawn))
            // .ForMember(dest => dest.StatsGoalsConceded, opt => opt.MapFrom(src => src.stats_goals_conceded))
            // .ForMember(dest => dest.StatsGoalsScored, opt => opt.MapFrom(src => src.stats_goals_scored))
            //.ForMember(dest => dest.StatsOtherAssists, opt => opt.MapFrom(src => src.stats_other_assists))
            // .ForMember(dest => dest.StatsOtherBlocks, opt => opt.MapFrom(src => src.stats_other_blocks))
            // .ForMember(dest => dest.StatsOtherClearances, opt => opt.MapFrom(src => src.stats_other_clearances))
            //.ForMember(dest => dest.StatsOtherHitWoodwork, opt => opt.MapFrom(src => src.stats_other_hit_woodwork))
            // .ForMember(dest => dest.StatsOtherInterceptions, opt => opt.MapFrom(src => src.stats_other_interceptions))
            // .ForMember(dest => dest.StatsOtherMinutesPlayed, opt => opt.MapFrom(src => src.stats_other_minutes_played))
            //.ForMember(dest => dest.StatsOtherOffsides, opt => opt.MapFrom(src => src.stats_other_offsides))
            // .ForMember(dest => dest.StatsOtherPenCommitted, opt => opt.MapFrom(src => src.stats_other_pen_committed))
            // .ForMember(dest => dest.StatsOtherPenMissed, opt => opt.MapFrom(src => src.stats_other_pen_missed))
            //.ForMember(dest => dest.StatsOtherPenSaved, opt => opt.MapFrom(src => src.stats_other_pen_saved))
            // .ForMember(dest => dest.StatsOtherPenScored, opt => opt.MapFrom(src => src.stats_other_pen_scored))
            // .ForMember(dest => dest.StatsOtherPenWon, opt => opt.MapFrom(src => src.stats_other_pen_won))
            //.ForMember(dest => dest.StatsOtherSaves, opt => opt.MapFrom(src => src.stats_other_saves))
            // .ForMember(dest => dest.StatsOtherTackles, opt => opt.MapFrom(src => src.stats_other_tackles))
            // .ForMember(dest => dest.StatsPassingCrossesAccuracy, opt => opt.MapFrom(src => src.stats_passing_crosses_accuracy))
            //.ForMember(dest => dest.StatsPassingPasses, opt => opt.MapFrom(src => src.stats_passing_passes))
            // .ForMember(dest => dest.StatsPassingPassesAccuracy, opt => opt.MapFrom(src => src.stats_passing_passes_accuracy))
            // .ForMember(dest => dest.StatsPassingTotalCrosses, opt => opt.MapFrom(src => src.stats_passing_total_crosses))
            //.ForMember(dest => dest.StatsShotsShotsOnGoal, opt => opt.MapFrom(src => src.stats_shots_shots_on_goal))
            // .ForMember(dest => dest.StatsShotsShotsTotal, opt => opt.MapFrom(src => src.stats_shots_shots_total))
             .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<SidelinedBusinessModel, SidelinedViewModel>();
            CreateMap<SidelinedBusinessModel, SidelinedV2ViewModel>();

            CreateMap<statistic, StatisticBusinessModel>()
           .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
           .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
           //.ForMember(dest => dest.AttacksAttacks, opt => opt.MapFrom(src => src.attacks_attacks))
           //.ForMember(dest => dest.BallSafe, opt => opt.MapFrom(src => src.ball_safe))
           //.ForMember(dest => dest.Corners, opt => opt.MapFrom(src => src.corners))
           //.ForMember(dest => dest.Fouls, opt => opt.MapFrom(src => src.fouls))
           //  .ForMember(dest => dest.FreeKick, opt => opt.MapFrom(src => src.free_kick))
           //.ForMember(dest => dest.GoalAttempts, opt => opt.MapFrom(src => src.goal_attempts))
           //.ForMember(dest => dest.GoalKick, opt => opt.MapFrom(src => src.goal_kick))
           //.ForMember(dest => dest.Offsides, opt => opt.MapFrom(src => src.offsides))
           //  .ForMember(dest => dest.PassesAccurate, opt => opt.MapFrom(src => src.passes_accurate))
           //.ForMember(dest => dest.PassesPercentage, opt => opt.MapFrom(src => src.passes_percentage))
           //.ForMember(dest => dest.PassesTotal, opt => opt.MapFrom(src => src.passes_total))
           // .ForMember(dest => dest.PossessionTime, opt => opt.MapFrom(src => src.possession_time))
           //.ForMember(dest => dest.RedCards, opt => opt.MapFrom(src => src.red_cards))
           // .ForMember(dest => dest.YellowCards, opt => opt.MapFrom(src => src.yellow_cards))
           //.ForMember(dest => dest.Saves, opt => opt.MapFrom(src => src.saves))
           // .ForMember(dest => dest.ShotsBlocked, opt => opt.MapFrom(src => src.shots_blocked))
           // .ForMember(dest => dest.ShotsInsideBox, opt => opt.MapFrom(src => src.shots_inside_box))
           //.ForMember(dest => dest.ShotsOffGoal, opt => opt.MapFrom(src => src.shots_off_goal))
           //.ForMember(dest => dest.ShotsOnGoal, opt => opt.MapFrom(src => src.shots_on_goal))
           // .ForMember(dest => dest.ShotsOutsideBox, opt => opt.MapFrom(src => src.shots_outside_box))
           // .ForMember(dest => dest.ShotsTotal, opt => opt.MapFrom(src => src.shots_total))
           //.ForMember(dest => dest.Substitutions, opt => opt.MapFrom(src => src.substitutions))
           // .ForMember(dest => dest.ThrowIn, opt => opt.MapFrom(src => src.throw_in))
           .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<StatisticBusinessModel, StatisticViewModel>().ReverseMap();
            CreateMap<StatisticBusinessModel, StatisticV2ViewModel>().ReverseMap();
            CreateMap<StatisticAnalysisBusinessModel, StatisticAnalysisV2ViewModel>();

            CreateMap<tvstation, TvstationBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<TvstationBusinessModel, TvstationViewModel>();
            CreateMap<TvstationBusinessModel, TvstationV2ViewModel>();

            CreateMap<venue, VenueBusinessModel>()
             .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
             .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            // .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.city))
            //.ForMember(dest => dest.Coordinates, opt => opt.MapFrom(src => src.coordinates))
             .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<VenueBusinessModel, VenueViewModel>();
            CreateMap<VenueBusinessModel, VenueV2ViewModel>();

            CreateMap<events, EventsBusinessModel>()
          .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
          .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
          .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<EventsBusinessModel, EventsViewModel>().ReverseMap();
            CreateMap<EventsBusinessModel, EventsV2ViewModel>().ReverseMap();

            CreateMap<odd, OddBusinessModel>()
           .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
           .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
           .ForMember(dest => dest.OddHandicap, opt => opt.MapFrom(src => src.handicap))
            .ForMember(dest => dest.OddLabel, opt => opt.MapFrom(src => src.label))
           .ForMember(dest => dest.OddTotal, opt => opt.MapFrom(src => src.total))
            .ForMember(dest => dest.OddValue, opt => opt.MapFrom(src => src.value))
             .ForMember(dest => dest.OddWinning, opt => opt.MapFrom(src => src.winning))
           .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<OddBusinessModel, OddViewModel>();
            CreateMap<OddViewModel, OddV2ViewModel>();

            CreateMap<market, MarketBusinessModel>()
          .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
          .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
          .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<MarketBusinessModel, MarketViewModel>();
            CreateMap<MarketViewModel, MarketV2ViewModel>();

            CreateMap<bookmaker, bookmakerBusinessModel>();
            CreateMap<bookmakerBusinessModel, bookmakerViewModel>();
            CreateMap<bookmakerBusinessModel, bookmakerV2ViewModel>();


            CreateMap<assistscorer, AssistsscorerBusinessModel>()
          .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
          .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
          .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<AssistsscorerBusinessModel, AssistsscorerViewModel>();

            CreateMap<cardscorer, CardscorerBusinessModel>()
           .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
           .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
           .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<CardscorerBusinessModel, CardscorerViewModel>();

            CreateMap<goalscorer, GoalscorerBusinessModel>()
         .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
         .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
         .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<GoalscorerBusinessModel, GoalscorerViewModel>();

            CreateMap<seasonstats, SeasonStatsBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            .ForMember(dest => dest.NumberOfClubs, opt => opt.MapFrom(src => src.number_of_clubs))
            .ForMember(dest => dest.NumberOfMatches, opt => opt.MapFrom(src => src.number_of_matches))
            .ForMember(dest => dest.NumberOfMatchesPlayed, opt => opt.MapFrom(src => src.number_of_matches_played))
            .ForMember(dest => dest.NumberOfGoals, opt => opt.MapFrom(src => src.number_of_goals))
            .ForMember(dest => dest.MatchesBothTeamsScored, opt => opt.MapFrom(src => src.matches_both_teams_scored))
            .ForMember(dest => dest.NumberOfYellowcards, opt => opt.MapFrom(src => src.number_of_yellowcards))
            .ForMember(dest => dest.NumberOfYellowredcards, opt => opt.MapFrom(src => src.number_of_yellowredcards))
            .ForMember(dest => dest.NumberOfRedcards, opt => opt.MapFrom(src => src.number_of_redcards))
            .ForMember(dest => dest.AvgGoalsPerMatch, opt => opt.MapFrom(src => src.avg_goals_per_match))
            .ForMember(dest => dest.AvgYellowcardsPerMatch, opt => opt.MapFrom(src => src.avg_yellowcards_per_match))
            .ForMember(dest => dest.AvgYellowredcardsPerMatch, opt => opt.MapFrom(src => src.avg_yellowredcards_per_match))
            .ForMember(dest => dest.AvgRedcardsPerMatch, opt => opt.MapFrom(src => src.avg_redcards_per_match))
            .ForMember(dest => dest.GoalsScoredMinutes0, opt => opt.MapFrom(src => src.goals_scored_minutes_0))
            .ForMember(dest => dest.GoalsScoredMinutes15, opt => opt.MapFrom(src => src.goals_scored_minutes_15))
            .ForMember(dest => dest.GoalsScoredMinutes30, opt => opt.MapFrom(src => src.goals_scored_minutes_30))
            .ForMember(dest => dest.GoalsScoredMinutes45, opt => opt.MapFrom(src => src.goals_scored_minutes_45))
            .ForMember(dest => dest.GoalsScoredMinutes60, opt => opt.MapFrom(src => src.goals_scored_minutes_60))
            .ForMember(dest => dest.GoalsScoredMinutes75, opt => opt.MapFrom(src => src.goals_scored_minutes_75))
            .ForMember(dest => dest.GoalScoredEveryMinutes, opt => opt.MapFrom(src => src.goal_scored_every_minutes))
            .ForMember(dest => dest.UpdateDateTime, opt => opt.MapFrom(src => src.update_date_time));
            CreateMap<SeasonStatsBusinessModel, SeasonStatsV2ViewModel>();

            CreateMap<prd_user, PrdUserBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
            .ForMember(dest => dest.FacebookId, opt => opt.MapFrom(src => src.facebook_id))
            .ForMember(dest => dest.GoogleId, opt => opt.MapFrom(src => src.google_id))
            .ForMember(dest => dest.Guid, opt => opt.MapFrom(src => src.guid))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.last_name))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
            .ForMember(dest => dest.NickName, opt => opt.MapFrom(src => src.nick_name))
            .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.password))
            .ForMember(dest => dest.TwitterId, opt => opt.MapFrom(src => src.twitter_id))
            .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.user_type));
            //CreateMap<PrdUserBusinessModel, PrdUserViewModel>();

            CreateMap<prd_coupon, PrdCouponBusinessModel>()
              .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
              .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time))
              .ForMember(dest => dest.IsWin, opt => opt.MapFrom(src => src.is_win))
              .ForMember(dest => dest.PrdUserId, opt => opt.MapFrom(src => src.prd_user_id))
              .ForMember(dest => dest.TotalRate, opt => opt.MapFrom(src => src.total_rate));

            CreateMap<prd_coupon_item, PrdCouponItemBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.FixtureId, opt => opt.MapFrom(src => src.fixture_id))
            .ForMember(dest => dest.MarketId, opt => opt.MapFrom(src => src.market_id))
            .ForMember(dest => dest.OddHandicap, opt => opt.MapFrom(src => src.odd_handicap))
            .ForMember(dest => dest.OddLabel, opt => opt.MapFrom(src => src.odd_label))
            .ForMember(dest => dest.OddTotal, opt => opt.MapFrom(src => src.odd_total))
            .ForMember(dest => dest.OddWinning, opt => opt.MapFrom(src => src.odd_winning))
            .ForMember(dest => dest.OddValue, opt => opt.MapFrom(src => src.odd_value))
            .ForMember(dest => dest.PrdCouponId, opt => opt.MapFrom(src => src.prd_coupon_id));

            CreateMap<prd_tips, TipsBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.OddId, opt => opt.MapFrom(src => src.odd_id))
            .ForMember(dest => dest.PrdUserId, opt => opt.MapFrom(src => src.prd_user_id))
            .ForMember(dest => dest.OddValue, opt => opt.MapFrom(src => src.odd_value))
            .ForMember(dest => dest.IsWin, opt => opt.MapFrom(src => src.is_win));
            //.ForMember(dest => dest.CreatedDateTime, opt => opt.MapFrom(src => src.create_date_time)

            CreateMap<prd_fixture_of_day, FixtureOfDayBusinessModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
            .ForMember(dest => dest.CreateDateTime, opt => opt.MapFrom(src => src.create_date_time));
        }

        private void NullCheck<T>(T obj)
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(obj);
                if (value == null && IsNullableInteger(property))
                {
                   // Console.WriteLine($"{property.Name} is null.");
                }
                else if (property.PropertyType == typeof(int) && (int)value == 0)
                {
                   //Console.WriteLine($"{property.Name} is zero.");
                }
            }
        }

        private bool IsNullableInteger(System.Reflection.PropertyInfo property)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType);
            return type == typeof(int);
        }
    }
}
