using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Odd;
using PreOddsApi.BusinessLayer.ValueObjects;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class FixtureService : IFixtureService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly ICountryService _countryService;
        private readonly IContinentService _continentService;
        private readonly ILeagueService _leagueService;
        private readonly ISeasonService _seasonService;
        private readonly IStageService _stageService;
        private readonly IGroupService _groupService;
        private readonly IRoundService _roundService;
        private readonly ITeamService _teamService;
        private readonly ICoachService _coachService;
        private readonly ICommentService _commentService;
        private readonly IEventsService _eventsService;
        private readonly IBenchService _benchService;
        private readonly ICornerService _cornerService;
        private readonly IHighlightService _highlightService;
        private readonly ILineupService _lineupService;
        private readonly IRefereeService _refereeService;
        private readonly ISidelinedService _sidelinedService;
        private readonly IStatisticService _statisticService;
        private readonly ITvstationService _tvstationService;
        private readonly IVenueService _venueService;
        private readonly IStandingService _standingService;
        private readonly IOddService _oddService;
        private readonly IMarketService _marketService;
        private readonly IMapper _mapper;
        private readonly ICacheHelper _cacheHelper;
        private readonly int takeFixture = 100;

        public FixtureService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, ICountryService countryService, IContinentService continentService, ICommentService commentService, IBenchService benchService, ILeagueService leagueService,
            ICoachService coachService, ITeamService teamService, ISeasonService seasonService, IStageService stageService, IGroupService groupService, IRoundService roundService, IMarketService marketService,
            ICornerService cornerService, IHighlightService highlightService, ILineupService lineupService, IRefereeService refereeService, ISidelinedService sidelinedService, IOddService oddService,
            IStatisticService statisticService, ITvstationService tvstationService, IVenueService venueService, IEventsService eventsService, IStandingService standingService, IMapper mapper, ICacheHelper cacheHelper)
        {
            _unitOfWork = unitOfWork;
            _countryService = countryService;
            _continentService = continentService;
            _leagueService = leagueService;
            _seasonService = seasonService;
            _stageService = stageService;
            _groupService = groupService;
            _roundService = roundService;
            _teamService = teamService;
            _commentService = commentService;
            _benchService = benchService;
            _cornerService = cornerService;
            _highlightService = highlightService;
            _lineupService = lineupService;
            _refereeService = refereeService;
            _sidelinedService = sidelinedService;
            _statisticService = statisticService;
            _tvstationService = tvstationService;
            _venueService = venueService;
            _eventsService = eventsService;
            _coachService = coachService;
            _standingService = standingService;
            _oddService = oddService;
            _marketService = marketService;
            _mapper = mapper;
            _cacheHelper = cacheHelper;
        }

        public FixtureBusinessModel GetFixture(long fixtureId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureBusinessModel();
            }

            var fixture = _mapper.Map<FixtureBusinessModel>(_unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId));
            if (fixture == null)
            {
                return new FixtureBusinessModel();
            }

            if (fixture.TimeStatus == "AU" && fixture.TimeStatus == "Deleted")
            {
                return new FixtureBusinessModel();
            }

            AddTimeZone(fixture, zone);
            if (fixture.WeatherTemperatureUnit != null)
            {
                fixture.WeatherTemperatureCelsiusTemp = Math.Round(((fixture.WeatherTemperatureTemp - 32) * 5) / 9, MidpointRounding.AwayFromZero);
            }
            SetFixtureFullInformations(fixture, timeZone);
            return fixture;
        }

        public FixtureBusinessModel GetFixtureForCoupon(long fixtureId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureBusinessModel();
            }

            var fixture = _mapper.Map<FixtureBusinessModel>(_unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId));
            if (fixture.TimeStatus == "AU" && fixture.TimeStatus == "Deleted")
            {
                return new FixtureBusinessModel();
            }

            AddTimeZone(fixture, zone);
            return fixture;
        }

        public FixtureBusinessModel GetFixtureForTips(long fixtureId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureBusinessModel();
            }

            var fixture = _mapper.Map<FixtureBusinessModel>(_unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId));
            if (fixture.TimeStatus == "AU" && fixture.TimeStatus == "Deleted")
            {
                return new FixtureBusinessModel();
            }

            AddTimeZone(fixture, zone);
            fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
            fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
            fixture.League = _leagueService.GetLeague(fixture.LeagueId);
            return fixture;
        }

        public FixtureBusinessModel GetFixtureForFixtureOfDay(long fixtureId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureBusinessModel();
            }

            var fixture = _mapper.Map<FixtureBusinessModel>(_unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId));
            if (fixture.TimeStatus == "AU" && fixture.TimeStatus == "Deleted")
            {
                return new FixtureBusinessModel();
            }

            AddTimeZone(fixture, zone);
            fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
            fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
            fixture.League = _leagueService.GetLeague(fixture.LeagueId);
            fixture.Country = _countryService.GetCountry(fixture.League.CountryId);
            return fixture;
        }

        private void GetFixtureForDate(string date, string timeZone)
        {
            //double zone;
            //if (!double.TryParse(timeZone, out zone))
            //{
            //    return new FixtureForLiveBusinessModel();
            //}

            //DateTime endDate = DateTime.Parse(date).AddDays(1);
            //DateTime atDate = DateTime.Parse(date);
            //DateTime startDate = DateTime.Parse(date).AddDays(-1);
            //var fixtureDateList = (from f in _unitOfWork.Repository<fixture>().GetList(p => (p.time_starting_at_date == startDate.ToString("yyyy-MM-dd")) ||
            //              (p.time_starting_at_date == atDate.ToString("yyyy-MM-dd")) || (p.time_starting_at_date == endDate.ToString("yyyy-MM-dd")))
            //                       join lt in _unitOfWork.Repository<team>().GetList() on f.local_team_id equals lt.id
            //                       join vt in _unitOfWork.Repository<team>().GetList() on f.visitor_team_id equals vt.id
            //                       join l in _unitOfWork.Repository<league>().GetList(p => p.status == 1) on f.league_id equals l.id
            //                       join c in _unitOfWork.Repository<country>().GetList(p => p.status == 1) on l.country_id equals c.id
            //                       select new
            //                       {
            //                           CountryName = c.name,
            //                           LeagueName = l.name,
            //                           LocalTeamName = lt.name,
            //                           VisitorTeamName = vt.name,
            //                           LocalTeamLogoPath = lt.logo_path,
            //                           VisitorTeamLogoPath = vt.logo_path,
            //                           CreateDateTime = f.create_date_time,
            //                           EtScore = f.et_score,
            //                           FtScore = f.ft_score,
            //                           HtScore = f.ht_score,
            //                           Id = f.id,
            //                           LeagueId = f.league_id,
            //                           LocalTeamId = f.local_team_id,
            //                           LocalTeamPenScore = f.local_team_pen_score,
            //                           LocalTeamScore = f.local_team_score,
            //                           TimeAddedTime = f.time_added_time,
            //                           TimeExtraMinute = f.time_extra_minute,
            //                           TimeInjuryTime = f.time_injury_time,
            //                           TimeMinute = f.time_minute,
            //                           TimeStartingAtDate = f.time_starting_at_date,
            //                           TimeStartingAtDateTime = f.time_starting_at_date_time,
            //                           TimeStartingAtTime = f.time_starting_at_time,
            //                           //TimeStartingAtTimestamp = f.time_starting_at_timestamp,
            //                           //TimeStartingAtTimezone = f.time_starting_at_timezone,
            //                           TimeStatus = f.time_status,
            //                           UpdateDateTime = f.update_date_time,
            //                           VisitorTeamId = f.visitor_team_id,
            //                           VisitorTeamPenScore = f.visitor_team_pen_score,
            //                           VisitorTeamScore = f.visitor_team_score,
            //                           IddaaCode = 0
            //                       }).ToList();

            //foreach (var fixture in fixtureDateList)
            //{
            //    AddTimeZone(fixture, zone);
            //    if (fixture.TimeStartingAtDate != fixture.TimeStartingAtDate)
            //    {
            //        continue;
            //    }
            //}
        }

        private void GetFixtureForLeague(string date, string timeZone)
        {

        }

        public FixtureForLiveBusinessModel GetFixtureForLive(string date, int tarihSecim, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureForLiveBusinessModel();
            }

            DateTime currentUTCDate = DateTime.UtcNow;
            DateTime currentLocalDateTime = currentUTCDate.AddMinutes(zone);
            DateTime atDate = DateTime.Parse(date);
            if (tarihSecim == 0)
            {
                atDate = currentUTCDate.Date;
                if (currentUTCDate.Hour < 4)
                {
                    atDate = atDate.AddDays(-1);
                }
            }

            double localZoneSeconds = zone * 60;
            DateTime utcDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            int utcTimestamp = (int)(atDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            double utcNowTimestamp = (int)(currentUTCDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            double startTimestamp = utcTimestamp;

            double endTimestamp = 0;
            if (tarihSecim == 0)
            {
                endTimestamp = (startTimestamp + 108000);
            }
            else
            {
                startTimestamp = utcTimestamp - localZoneSeconds;
                endTimestamp = (startTimestamp + 86400);
            }

            var fixtureBase = _unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= startTimestamp && p.startingAtTimestamp < endTimestamp);
            FixtureForLiveBusinessModel fixtureLive = new FixtureForLiveBusinessModel();

            var fixtureLeagueList = _mapper.Map<List<FixtureForLeagueBusinessModel>>(fixtureBase).OrderBy(p => p.TimeStartingAtDate).ToList();
            var fixtureLeagueGroup = fixtureLeagueList.GroupBy(p => p.LeagueId);
            List<FixtureForLeagueBaseBusinessModel> leagueBaseList = new List<FixtureForLeagueBaseBusinessModel>();
            List<FixtureForLeagueBaseBusinessModel> leagueliveBaseList = new List<FixtureForLeagueBaseBusinessModel>();
            FixtureForDateBaseBusinessModel leagueDateList = new FixtureForDateBaseBusinessModel();

            var leagueList = (List<LeagueBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Leagues_Live, date, "_live"));
            if (leagueList == null)
            {
                leagueList = new List<LeagueBusinessModel>();
            }
            bool leagueCacheReset = false;

            var countryList = (List<CountryBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Countries_Live, date, "_live"));
            if (countryList == null)
            {
                countryList = new List<CountryBusinessModel>();
            }
            bool countryCacheReset = false;

            var teamList = (List<TeamBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Teams_Live, date, "_live"));
            if (teamList == null)
            {
                teamList = new List<TeamBusinessModel>();
            }
            bool teamCacheReset = false;

            foreach (var leagueGroup in fixtureLeagueGroup)
            {
                LeagueBusinessModel league = leagueList.SingleOrDefault(p => p.Id == leagueGroup.Key); //_leagueService.GetLeague(leagueGroup.Key, statu);
                if (league == null)
                {
                    league = _leagueService.GetLeague(leagueGroup.Key);
                    if (league == null)
                    {
                        continue;
                    }
                    else
                    {
                        leagueList.Add(league);
                        leagueCacheReset = true;
                    }
                }

                CountryBusinessModel country = countryList.FirstOrDefault(p => p.Id == league.CountryId); //_countryService.GetCountry(league.CountryId);
                if (country == null)
                {
                    country = _countryService.GetCountry(league.CountryId);
                    if (country == null)
                    {
                        continue;
                    }
                    else
                    {
                        countryList.Add(country);
                        countryCacheReset = true;
                    }
                }

                //if (!fixtureLive.Countries.Contains(country))
                //{
                //    fixtureLive.Countries.Add(country);
                //}

                FixtureForLeagueBaseBusinessModel fixtureForLeagueBase = new FixtureForLeagueBaseBusinessModel();
                FixtureForLeagueBaseBusinessModel fixtureForLeagueLiveBase = new FixtureForLeagueBaseBusinessModel();
                if (league.Cup)
                {
                    var fixtureGroup = fixtureLeagueList.Where(p => p.LeagueId == leagueGroup.Key).GroupBy(p => p.GroupId);
                    foreach (var group in fixtureGroup.OrderBy(p => p.Key))
                    {
                        GroupBusinessModel currentGroup = _groupService.GetGroup(group.Key);
                        league = _leagueService.GetLeague(leagueGroup.Key);
                        if (currentGroup != null)
                        {
                            league.Name += " - " + currentGroup.Name;
                        }


                        fixtureForLeagueBase = new FixtureForLeagueBaseBusinessModel();
                        fixtureForLeagueBase.Group = currentGroup;
                        fixtureForLeagueBase.League = league;
                        fixtureForLeagueBase.Country = country;

                        fixtureForLeagueLiveBase = new FixtureForLeagueBaseBusinessModel();
                        fixtureForLeagueLiveBase.Group = currentGroup;
                        fixtureForLeagueLiveBase.League = league;
                        fixtureForLeagueLiveBase.Country = country;

                        foreach (var fixture in fixtureLeagueList.Where(p => p.LeagueId == leagueGroup.Key && p.GroupId == group.Key).OrderBy(p => p.TimeStartingAtTimestamp))
                        {
                            if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                            {
                                continue;
                            }

                            AddTimeZone(fixture, zone);
                            //fixture.LocalTeam = teamList.FirstOrDefault(p => p.Id == fixture.LocalTeamId); //_teamService.GetTeam(fixture.LocalTeamId);
                            //if (fixture.LocalTeam == null)
                            //{
                            //    fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                            //}

                            //fixture.VisitorTeam = teamList.FirstOrDefault(p => p.Id == fixture.VisitorTeamId); //_teamService.GetTeam(fixture.VisitorTeamId);
                            //if (fixture.VisitorTeam == null)
                            //{
                            //    fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                            //}

                            //if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                            //{
                            //    continue;
                            //}

                            fixture.LocalTeam = teamList.FirstOrDefault(p => p.Id == fixture.LocalTeamId); //_teamService.GetTeam(fixture.LocalTeamId);
                            if (fixture.LocalTeam == null)
                            {
                                fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                                if (fixture.LocalTeam != null)
                                {
                                    teamList.Add(fixture.LocalTeam);
                                    teamCacheReset = true;
                                }
                            }

                            fixture.VisitorTeam = teamList.FirstOrDefault(p => p.Id == fixture.VisitorTeamId); //_teamService.GetTeam(fixture.VisitorTeamId);
                            if (fixture.VisitorTeam == null)
                            {
                                fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                                if (fixture.VisitorTeam != null)
                                {
                                    teamList.Add(fixture.VisitorTeam);
                                    teamCacheReset = true;
                                }
                            }

                            if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                            {
                                continue;
                            }

                            fixtureForLeagueBase.Fixture.Add(fixture);

                            if (fixture.TimeStatus == "LIVE" || fixture.TimeStatus == "HT" || fixture.TimeStatus == "ET" || fixture.TimeStatus == "PEN_LIVE" || fixture.TimeStatus == "BREAK")
                            {
                                fixtureForLeagueLiveBase.Fixture.Add(fixture);
                            }

                            FixtureForLeagueBusinessModel fixtureForDate = new FixtureForLeagueBusinessModel();
                            fixtureForDate = fixture;
                            fixtureForDate.Country = country;
                            fixtureForDate.League = league;
                            leagueDateList.Fixture.Add(fixtureForDate);
                        }

                        if (fixtureForLeagueBase.Fixture.Count > 0)
                        {
                            fixtureForLeagueBase.MatchCount = fixtureForLeagueBase.Fixture.Count;
                            fixtureLive.MatchCount += fixtureForLeagueBase.Fixture.Count;
                            if (fixtureForLeagueLiveBase.Fixture.Count > 0)
                            {
                                fixtureForLeagueBase.LiveMatchCount = fixtureForLeagueLiveBase.Fixture.Count;
                                fixtureLive.LiveMatchCount += fixtureForLeagueLiveBase.Fixture.Count;
                            }
                            else
                            {
                                fixtureForLeagueBase.LiveMatchCount = 0;
                            }
                            leagueBaseList.Add(fixtureForLeagueBase);
                        }

                        if (fixtureForLeagueLiveBase.Fixture.Count > 0)
                        {
                            fixtureForLeagueLiveBase.LiveMatchCount = fixtureForLeagueLiveBase.Fixture.Count;
                            fixtureForLeagueLiveBase.MatchCount = fixtureForLeagueBase.Fixture.Count;
                            leagueliveBaseList.Add(fixtureForLeagueLiveBase);
                        }
                    }
                }
                else
                {
                    fixtureForLeagueBase.League = league;
                    fixtureForLeagueBase.Country = country;
                    fixtureForLeagueLiveBase.League = league;
                    fixtureForLeagueLiveBase.Country = country;

                    foreach (var fixture in fixtureLeagueList.Where(p => p.LeagueId == leagueGroup.Key).OrderBy(p => p.TimeStartingAtTimestamp))
                    {
                        if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                        {
                            continue;
                        }

                        AddTimeZone(fixture, zone);
                        //fixture.LocalTeam = teamList.FirstOrDefault(p => p.Id == fixture.LocalTeamId); //_teamService.GetTeam(fixture.LocalTeamId);
                        //if (fixture.LocalTeam == null)
                        //{
                        //    fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                        //}

                        //fixture.VisitorTeam = teamList.FirstOrDefault(p => p.Id == fixture.VisitorTeamId); //_teamService.GetTeam(fixture.VisitorTeamId);
                        //if (fixture.VisitorTeam == null)
                        //{
                        //    fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                        //}

                        //if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                        //{
                        //    continue;
                        //}

                        fixture.LocalTeam = teamList.FirstOrDefault(p => p.Id == fixture.LocalTeamId); //_teamService.GetTeam(fixture.LocalTeamId);
                        if (fixture.LocalTeam == null)
                        {
                            fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                            if (fixture.LocalTeam != null)
                            {
                                teamList.Add(fixture.LocalTeam);
                                teamCacheReset = true;
                            }
                        }

                        fixture.VisitorTeam = teamList.FirstOrDefault(p => p.Id == fixture.VisitorTeamId); //_teamService.GetTeam(fixture.VisitorTeamId);
                        if (fixture.VisitorTeam == null)
                        {
                            fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                            if (fixture.VisitorTeam != null)
                            {
                                teamList.Add(fixture.VisitorTeam);
                                teamCacheReset = true;
                            }
                        }

                        if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                        {
                            continue;
                        }

                        fixtureForLeagueBase.Fixture.Add(fixture);
                        if (fixture.TimeStatus == "LIVE" || fixture.TimeStatus == "HT" || fixture.TimeStatus == "ET" || fixture.TimeStatus == "PEN_LIVE" || fixture.TimeStatus == "BREAK")
                        {
                            fixtureForLeagueLiveBase.Fixture.Add(fixture);
                        }
                        FixtureForLeagueBusinessModel fixtureForDate = new FixtureForLeagueBusinessModel();
                        fixtureForDate = fixture;
                        fixtureForDate.Country = country;
                        fixtureForDate.League = league;
                        leagueDateList.Fixture.Add(fixtureForDate);
                    }

                    if (fixtureForLeagueBase.Fixture.Count > 0)
                    {
                        fixtureForLeagueBase.MatchCount = fixtureForLeagueBase.Fixture.Count;
                        fixtureLive.MatchCount += fixtureForLeagueBase.Fixture.Count;
                        if (fixtureForLeagueLiveBase.Fixture.Count > 0)
                        {
                            fixtureForLeagueBase.LiveMatchCount = fixtureForLeagueLiveBase.Fixture.Count;
                            fixtureLive.LiveMatchCount += fixtureForLeagueLiveBase.Fixture.Count;
                        }
                        else
                        {
                            fixtureForLeagueBase.LiveMatchCount = 0;
                        }
                        leagueBaseList.Add(fixtureForLeagueBase);
                    }

                    if (fixtureForLeagueLiveBase.Fixture.Count > 0)
                    {
                        fixtureForLeagueLiveBase.LiveMatchCount = fixtureForLeagueLiveBase.Fixture.Count;
                        fixtureForLeagueLiveBase.MatchCount = fixtureForLeagueBase.Fixture.Count;
                        leagueliveBaseList.Add(fixtureForLeagueLiveBase);
                    }
                }
            }

            fixtureLive.FixtureForLeague = leagueBaseList.OrderByDescending(p => p.League.Favorite).ThenBy(p => p.League.Id).ThenByDescending(p => p.League.LegacyId).ToList();
            fixtureLive.FixtureForLeagueLive = leagueliveBaseList.OrderByDescending(p => p.League.Favorite).ThenBy(p => p.League.Id).ThenByDescending(p => p.League.LegacyId).ToList();
            fixtureLive.FixtureForDate.Fixture = leagueDateList.Fixture.OrderBy(p => p.TimeStartingAtTimestamp).ToList();
            fixtureLive.Countries = countryList.OrderBy(p => p.Id).ToList();

            if (leagueCacheReset)
            {
                _cacheHelper.Set(string.Format(CacheKeys.Leagues_Live, date, "_live"), leagueList, 45 * 60 * 60);
            }

            if (countryCacheReset)
            {
                _cacheHelper.Set(string.Format(CacheKeys.Countries_Live, date, "_live"), countryList, 45 * 60 * 60);
            }

            if (teamCacheReset)
            {
                _cacheHelper.Set(string.Format(CacheKeys.Teams_Live, date, "_live"), teamList, 45 * 60 * 60);
            }

            return fixtureLive;
        }

        public FixtureForLiveBusinessModel GetFixtureForLiveV2(string date, int tarihSecim, int statu, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureForLiveBusinessModel();
            }

            DailyTimeZoneVO dailyTimeZone = calculateTimezone(date, tarihSecim, statu, zone);

            var fixtureBase = _unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= dailyTimeZone.startTimestamp && p.startingAtTimestamp < dailyTimeZone.endTimestamp);

            var fixtureLeagueList = _mapper.Map<List<FixtureForLeagueBusinessModel>>(fixtureBase).OrderBy(p => p.TimeStartingAtDate).ToList();
            var fixtureLeagueGroup = fixtureLeagueList.GroupBy(p => p.LeagueId);

            FixtureForLiveBusinessModel fixtureLive = new FixtureForLiveBusinessModel();
            List<FixtureForLeagueBaseBusinessModel> leagueBaseList = new List<FixtureForLeagueBaseBusinessModel>();
            List<FixtureForLeagueBaseBusinessModel> leagueliveBaseList = new List<FixtureForLeagueBaseBusinessModel>();

            var leagueList = (List<LeagueBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Leagues, date));
            if (leagueList == null)
            {
                leagueList = new List<LeagueBusinessModel>();
            }
            bool leagueCacheReset = false;

            var countryList = (List<CountryBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Countries, date));
            if (countryList == null)
            {
                countryList = new List<CountryBusinessModel>();
            }
            bool countryCacheReset = false;

            var teamList = (List<TeamBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Teams, date));
            if (teamList == null)
            {
                teamList = new List<TeamBusinessModel>();
            }
            bool teamCacheReset = false;

            foreach (var leagueGroup in fixtureLeagueGroup)
            {
                LeagueBusinessModel league = leagueList.SingleOrDefault(p => p.Id == leagueGroup.Key && p.Status == statu); //_leagueService.GetLeague(leagueGroup.Key, statu);
                if (league == null)
                {
                    league = _leagueService.GetLeague(leagueGroup.Key, statu);
                    if (league == null)
                    {
                        continue;
                    }
                    else
                    {
                        leagueList.Add(league);
                        leagueCacheReset = true;
                    }
                }

                CountryBusinessModel country = countryList.FirstOrDefault(p => p.Id == league.CountryId); //_countryService.GetCountry(league.CountryId);
                if (country == null)
                {
                    country = _countryService.GetCountry(league.CountryId);
                    if (country == null)
                    {
                        continue;
                    }
                    else
                    {
                        countryList.Add(country);
                        countryCacheReset = true;
                    }
                }

                FixtureForLeagueBaseBusinessModel fixtureForLeagueBase = new FixtureForLeagueBaseBusinessModel();
                FixtureForLeagueBaseBusinessModel fixtureForLeagueLiveBase = new FixtureForLeagueBaseBusinessModel();
                if (league.Cup)
                {
                    var fixtureGroup = fixtureLeagueList.Where(p => p.LeagueId == leagueGroup.Key).GroupBy(p => p.GroupId);
                    foreach (var group in fixtureGroup.OrderBy(p => p.Key))
                    {
                        GroupBusinessModel currentGroup = _groupService.GetGroup(group.Key);
                        league = _leagueService.GetLeague(leagueGroup.Key);
                        if (currentGroup != null)
                        {
                            league.Name += " - " + currentGroup.Name;
                        }

                        fixtureForLeagueBase = new FixtureForLeagueBaseBusinessModel();
                        fixtureForLeagueBase.League = league;
                        fixtureForLeagueBase.Country = country;

                        fixtureForLeagueLiveBase = new FixtureForLeagueBaseBusinessModel();
                        fixtureForLeagueLiveBase.League = league;
                        fixtureForLeagueLiveBase.Country = country;

                        foreach (var fixture in fixtureLeagueList.Where(p => p.LeagueId == leagueGroup.Key && p.GroupId == group.Key).OrderBy(p => p.TimeStartingAtTimestamp))
                        {
                            if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                            {
                                continue;
                            }

                            AddTimeZone(fixture, zone);
                            fixture.LocalTeam = teamList.FirstOrDefault(p => p.Id == fixture.LocalTeamId); //_teamService.GetTeam(fixture.LocalTeamId);
                            if (fixture.LocalTeam == null)
                            {
                                fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                                if (fixture.LocalTeam != null)
                                {
                                    teamList.Add(fixture.LocalTeam);
                                    teamCacheReset = true;
                                }
                            }

                            fixture.VisitorTeam = teamList.FirstOrDefault(p => p.Id == fixture.VisitorTeamId); //_teamService.GetTeam(fixture.VisitorTeamId);
                            if (fixture.VisitorTeam == null)
                            {
                                fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                                if (fixture.VisitorTeam != null)
                                {
                                    teamList.Add(fixture.VisitorTeam);
                                    teamCacheReset = true;
                                }
                            }

                            if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                            {
                                continue;
                            }

                            fixtureForLeagueBase.Fixture.Add(fixture);

                            if (fixture.TimeStatus == "LIVE" || fixture.TimeStatus == "HT" || fixture.TimeStatus == "ET" || fixture.TimeStatus == "PEN_LIVE" || fixture.TimeStatus == "BREAK")
                            {
                                fixtureForLeagueLiveBase.Fixture.Add(fixture);
                            }
                        }

                        if (fixtureForLeagueBase.Fixture.Count > 0)
                        {
                            leagueBaseList.Add(fixtureForLeagueBase);
                        }

                        if (fixtureForLeagueLiveBase.Fixture.Count > 0)
                        {
                            leagueliveBaseList.Add(fixtureForLeagueLiveBase);
                        }
                    }
                }
                else
                {
                    fixtureForLeagueBase.League = league;
                    fixtureForLeagueBase.Country = country;
                    fixtureForLeagueLiveBase.League = league;
                    fixtureForLeagueLiveBase.Country = country;

                    foreach (var fixture in fixtureLeagueList.Where(p => p.LeagueId == leagueGroup.Key).OrderBy(p => p.TimeStartingAtTimestamp))
                    {
                        if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                        {
                            continue;
                        }

                        AddTimeZone(fixture, zone);
                        fixture.LocalTeam = teamList.FirstOrDefault(p => p.Id == fixture.LocalTeamId); //_teamService.GetTeam(fixture.LocalTeamId);
                        if (fixture.LocalTeam == null)
                        {
                            fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                            if (fixture.LocalTeam != null)
                            {
                                teamList.Add(fixture.LocalTeam);
                                teamCacheReset = true;
                            }
                        }

                        fixture.VisitorTeam = teamList.FirstOrDefault(p => p.Id == fixture.VisitorTeamId); //_teamService.GetTeam(fixture.VisitorTeamId);
                        if (fixture.VisitorTeam == null)
                        {
                            fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                            if (fixture.VisitorTeam != null)
                            {
                                teamList.Add(fixture.VisitorTeam);
                                teamCacheReset = true;
                            }
                        }

                        //fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                        //fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                        if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                        {
                            continue;
                        }

                        fixtureForLeagueBase.Fixture.Add(fixture);

                        if (fixture.TimeStatus == "LIVE" || fixture.TimeStatus == "HT" || fixture.TimeStatus == "ET" || fixture.TimeStatus == "PEN_LIVE" || fixture.TimeStatus == "BREAK")
                        {
                            fixtureForLeagueLiveBase.Fixture.Add(fixture);
                        }
                    }

                    if (fixtureForLeagueBase.Fixture.Count > 0)
                    {
                        leagueBaseList.Add(fixtureForLeagueBase);
                    }

                    if (fixtureForLeagueLiveBase.Fixture.Count > 0)
                    {
                        leagueliveBaseList.Add(fixtureForLeagueLiveBase);
                    }
                }
            }

            fixtureLive.FixtureForLeague = leagueBaseList.OrderByDescending(p => p.League.Favorite).ThenBy(p => p.League.Id).ThenByDescending(p => p.League.LegacyId).ToList();
            fixtureLive.FixtureForLeagueLive = leagueliveBaseList.OrderByDescending(p => p.League.Favorite).ThenBy(p => p.League.Id).ThenByDescending(p => p.League.LegacyId).ToList();

            if (leagueCacheReset)
            {
                _cacheHelper.Set(string.Format(CacheKeys.Leagues, date), leagueList, 45 * 60 * 60);
            }

            if (countryCacheReset)
            {
                _cacheHelper.Set(string.Format(CacheKeys.Countries, date), countryList, 45 * 60 * 60);
            }

            if (teamCacheReset)
            {
                _cacheHelper.Set(string.Format(CacheKeys.Teams, date), teamList, 45 * 60 * 60);
            }

            return fixtureLive;
        }

        public FixtureForFavoriteBusinessModel GetFavoriteFixture(string fixtureIds, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureForFavoriteBusinessModel();
            }

            List<string> ids = fixtureIds.Split(',').ToList<string>();

            FixtureForFavoriteBusinessModel fixtureFavoriteList = new FixtureForFavoriteBusinessModel();
            foreach (var id in ids)
            {
                long currentId;
                if (!long.TryParse(id, out currentId))
                {
                    return new FixtureForFavoriteBusinessModel();
                }

                var fixture = _mapper.Map<FixtureForLeagueBusinessModel>(_unitOfWork.Repository<fixture>().Get(p => p.id == currentId));

                LeagueBusinessModel league = _leagueService.GetLeague(fixture.LeagueId);
                if (league == null)
                {
                    continue;
                }

                CountryBusinessModel country = _countryService.GetCountry(league.CountryId);
                if (country == null)
                {
                    continue;
                }

                AddTimeZone(fixture, zone);
                fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);

                fixtureFavoriteList.Fixture.Add(fixture);
            }

            return fixtureFavoriteList;
        }

        public List<FixtureForLeagueBaseBusinessModel> GetFixtureByRoundId(long leagueId, long seasonId, long stageId, long groupId, long roundId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new List<FixtureForLeagueBaseBusinessModel>();
            }

            List<FixtureForLeagueBusinessModel> fixtures;
            if (stageId != 0)
            {
                if (roundId != 0)
                {
                    if (groupId != 0)
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId && p.groupId== groupId && p.roundId == roundId));
                    }
                    else
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId && p.roundId == roundId));
                    }
                }
                else
                {
                    if (groupId != 0)
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId && p.groupId== groupId));
                    }
                    else
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId));
                    }
                }
            }
            else
            {
                if (roundId != 0)
                {
                    if (groupId != 0)
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.groupId== groupId && p.roundId == roundId));
                    }
                    else
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.roundId == roundId));
                    }
                }
                else
                {
                    if (groupId != 0)
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.groupId== groupId));
                    }
                    else
                    {
                        fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>()
                            .GetList(p => p.leagueId == leagueId && p.seasonId== seasonId));
                    }
                }
            }

            Parallel.ForEach(fixtures, fixture =>
            {
                AddTimeZone(fixture, zone);
            });

            var fixtureGroup = fixtures.GroupBy(p => p.TimeStartingAtDate);
            List<FixtureForLeagueBaseBusinessModel> baseModelList = new List<FixtureForLeagueBaseBusinessModel>();
            foreach (var group in fixtureGroup.OrderBy(p => p.Key))
            {
                FixtureForLeagueBaseBusinessModel baseModel = new FixtureForLeagueBaseBusinessModel();
                baseModel.TimeStartingAtDate = group.Key;
                foreach (var fixture in fixtures.Where(p => p.TimeStartingAtDate == group.Key).OrderBy(p => p.TimeStartingAtTime))
                {
                    if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                    {
                        continue;
                    }

                    //AddTimeZone(fixture, zone);
                    SetFixtureInformationsForLeague(fixture);
                    baseModel.Fixture.Add(fixture);
                }

                baseModelList.Add(baseModel);
            }

            return baseModelList;
        }

        public List<FixtureForLeagueBusinessModel> GetFixturesOfRound(long leagueId, long seasonId, long stageId, long roundId, long groupId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new List<FixtureForLeagueBusinessModel>();
            }

            var season = _seasonService.GetSeason(seasonId);

            if (season.CurrentSeason)
            {
                if (stageId != 0)
                {
                    if (groupId != 0)
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId && p.stageId == stageId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }

                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                           p.seasonId== seasonId && p.stageId == stageId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }

                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                    }
                    else
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId && p.stageId == stageId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.stageId == stageId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                    }
                }
                else
                {
                    if (groupId != 0)
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }

                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                           p.seasonId== seasonId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }

                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                    }
                    else
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                    }
                }
            }
            else
            {
                if (stageId != 0)
                {
                    if (groupId != 0)
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId && p.stageId == stageId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                           p.seasonId== seasonId && p.stageId == stageId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }

                    }
                    else
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId && p.stageId == stageId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                           p.seasonId== seasonId && p.stageId == stageId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                    }
                }
                else
                {
                    if (groupId != 0)
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                           p.seasonId== seasonId && p.groupId== groupId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }

                    }
                    else
                    {
                        if (roundId != 0)
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                            p.seasonId== seasonId && p.roundId == roundId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                        else
                        {
                            var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId &&
                           p.seasonId== seasonId));
                            foreach (var fixtureItem in fixtures)
                            {
                                if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                                {
                                    continue;
                                }
                                AddTimeZone(fixtureItem, zone);
                                SetFixtureInformationsForLeague(fixtureItem);
                            }
                            return fixtures;
                        }
                    }
                }
            }
        }

        public List<FixtureForLeagueBusinessModel> GetFixturesOfRound(long leagueId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new List<FixtureForLeagueBusinessModel>();
            }

            //FixtureForRoundBusinessModel fixture = new FixtureForRoundBusinessModel();
            //var league = _mapper.Map<LeagueBusinessModel>(_unitOfWork.Repository<league>().Get(p => p.id == leagueId));
            //fixture.League = league;
            var season = _seasonService.GetCurrentSeason(leagueId);
            //var season = seasons.FirstOrDefault(p => p.CurrentSeason == true);
            //fixture.Seasons = seasons;
            //fixture.Season = season;
            var stage = _stageService.GetStage(season.CurrentStageId);
            //var stages = _mapper.Map<List<StageBusinessModel>>(_unitOfWork.Repository<stage>().GetList(p => p.seasonId== season.Id));
            //fixture.Stages = stages;
            //fixture.Stage = stage;
            var round = _roundService.GetRound(season.CurrentRoundId);
            //var rounds = _mapper.Map<List<RoundBusinessModel>>(_unitOfWork.Repository<round>().GetList(p => p.stageId == season.CurrentStageId));
            //fixture.Rounds = rounds;
            //fixture.Round = round;
            var group = _groupService.GetGroup(season.CurrentRoundId, season.CurrentStageId);
            //fixture.Group = group;

            if (group != null)
            {
                var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId && p.seasonId== season.Id && p.roundId == season.CurrentRoundId && p.stageId == season.CurrentStageId && p.groupId== group.Id));
                foreach (var fixtureItem in fixtures)
                {
                    if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                    {
                        continue;
                    }
                    AddTimeZone(fixtureItem, zone);
                    SetFixtureInformationsForLeague(fixtureItem);
                }

                return fixtures;
            }
            else
            {
                var fixtures = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.leagueId == leagueId && p.seasonId== season.Id && p.roundId == season.CurrentRoundId && p.stageId == season.CurrentStageId));
                foreach (var fixtureItem in fixtures)
                {
                    if (fixtureItem.TimeStatus == "AU" || fixtureItem.TimeStatus == "Deleted")
                    {
                        continue;
                    }
                    AddTimeZone(fixtureItem, zone);
                    SetFixtureInformationsForLeague(fixtureItem);
                }
                return fixtures;
            }

        }

        public FixtureDetailHeaderBusinessModel GetFixtureDetailHeader(long fixtureId, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new FixtureDetailHeaderBusinessModel();
            }


            FixtureDetailHeaderBusinessModel fixture = _mapper.Map<FixtureDetailHeaderBusinessModel>(_unitOfWork.Repository<fixture>().Get(p => p.id == fixtureId));
            if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
            {
                return new FixtureDetailHeaderBusinessModel();
            }
            fixture.League = _leagueService.GetLeague(fixture.LeagueId);
            fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
            fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
            if (fixture.LocalTeamCoachId != 0)
            {
                fixture.LocalTeamCoach = _coachService.GetCoach(fixture.LocalTeamCoachId);
            }
            if (fixture.VisitorTeamCoachId != 0)
            {
                fixture.VisitorTeamCoach = _coachService.GetCoach(fixture.VisitorTeamCoachId);
            }

            AddTimeZone(fixture, zone);

            return fixture;
        }

        public FixtureForOddAnalysisBaseBusinessModel GetHotRateFixtures(string date, long bookmakerId, long marketId, int winningPercent, int earningPercente, string part, string rate, int allFixture, int page, string timeZone)
        {
            FixtureForOddAnalysisBaseBusinessModel oddAnalysisBaseModel = new FixtureForOddAnalysisBaseBusinessModel();
            oddAnalysisBaseModel.Page = page;
            try
            {
                double zone;
                if (!double.TryParse(timeZone, out zone))
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                    oddAnalysisBaseModel.Success = true;
                    return oddAnalysisBaseModel;
                }

                DateTime currentUTCDate = DateTime.UtcNow;
                DateTime currentLocalDateTime = currentUTCDate.AddMinutes(zone);
                DateTime atDate = DateTime.Parse(date);

                double localZoneSeconds = zone * 60;
                DateTime utcDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                int utcTimestamp = (int)(atDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double localNowTimestamp = (int)(currentLocalDateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double utcNowTimestamp = (int)(currentUTCDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double startTimestamp = utcTimestamp - localZoneSeconds;
                double endTimestamp = (startTimestamp + 86400);

                int odd_value = GetValueFromAnalysisRatePart(Enum.Parse<AnalysisRatePart>(rate));
                int count = GetValueFromAnalysisPart(Enum.Parse<AnalysisPart>(part));

                List<FixtureForOddAnalysisBusinessModel> fixtures;
                if (allFixture == 0)
                {
                    fixtures = _mapper.Map<List<FixtureForOddAnalysisBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= utcNowTimestamp && p.startingAtTimestamp >= startTimestamp && p.startingAtTimestamp < endTimestamp).OrderBy(p => p.startingAtTimestamp).Skip(page * takeFixture).Take(takeFixture));
                }
                else
                {
                    fixtures = _mapper.Map<List<FixtureForOddAnalysisBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= startTimestamp && p.startingAtTimestamp < endTimestamp).OrderBy(p => p.startingAtTimestamp).Skip(page * takeFixture).Take(takeFixture));
                }

                List<FixtureForOddAnalysisBusinessModel> hotRateFixtures = new List<FixtureForOddAnalysisBusinessModel>();

                var hotRateList = _oddService.GetHotRateOdds(DateTime.Parse(date).AddDays(-1).ToString("yyyy-MM-dd"), bookmakerId, marketId, winningPercent, earningPercente, count, odd_value);
                if (hotRateList.Count == 0)
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                    oddAnalysisBaseModel.Success = true;
                    return oddAnalysisBaseModel;
                }

                if (fixtures.Count < takeFixture)
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                }
                else
                {
                    oddAnalysisBaseModel.IsLastPage = false;
                }

                var leagueList = (List<LeagueBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Leagues_Live, date, "_live"));
                var countryList = (List<CountryBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Countries_Live, date, "_live"));

                List<MarketBusinessModel> markets = _marketService.GetMarkets();
                foreach (var fixture in fixtures.OrderBy(p => p.TimeStartingAtTimestamp))
                {
                    if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                    {
                        continue;
                    }

                    LeagueBusinessModel league = null;
                    if (leagueList != null)
                    {
                        league = leagueList.SingleOrDefault(p => p.Id == fixture.LeagueId);
                        if (league == null)
                        {
                            league = _leagueService.GetLeague(fixture.LeagueId);
                            if (league == null)
                            {
                                continue;
                            }
                            else
                            {
                                fixture.League = league;
                            }
                        }
                        else
                        {
                            fixture.League = league;
                        }
                    }
                    else
                    {
                        league = _leagueService.GetLeague(fixture.LeagueId);
                        if (league == null)
                        {
                            continue;
                        }
                        else
                        {
                            fixture.League = league;
                        }
                    }

                    CountryBusinessModel country = null;
                    if (countryList != null)
                    {
                        country = countryList.FirstOrDefault(p => p.Id == fixture.League.CountryId); //_countryService.GetCountry(league.CountryId);
                        if (country == null)
                        {
                            country = _countryService.GetCountry(fixture.League.CountryId);
                            if (country == null)
                            {
                                continue;
                            }
                            else
                            {
                                fixture.Country = country;
                            }
                        }
                        else
                        {
                            fixture.Country = country;
                        }
                    }
                    else
                    {
                        country = _countryService.GetCountry(fixture.League.CountryId);
                        if (country == null)
                        {
                            continue;
                        }
                        else
                        {
                            fixture.Country = country;
                        }
                    }

                    List<OddBusinessModel> odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixture.Id && p.bookmakerId == bookmakerId && p.marketId == marketId));

                    if (odds.Count > 0)
                    {
                        Parallel.ForEach(odds, odd =>
                        {
                            if (odd.OddValue != "0")
                            {
                                decimal oddGroupPercent = CalculateOddGroupPercent(odd, odds);
                                HotRateBusinessModel oddAnalysis = hotRateList.Where(p => p.OddLabel == odd.OddLabel && p.OddTotal == (string.IsNullOrEmpty(odd.OddTotal) ? null : odd.OddTotal)
                                && p.OddValue == odd.OddValue && p.OddHandicap == (string.IsNullOrEmpty(odd.OddHandicap) ? null : odd.OddHandicap)).OrderBy(p=>p.Id).FirstOrDefault();
                                int currentOddValue = int.Parse(odd.OddValue.Replace(".", ""));
                                if (odd.OddValue.Length == 1)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "00");
                                }
                                else if (odd.OddValue.Length == 2)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "00");
                                }
                                else if (odd.OddValue.Length == 3)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "0");
                                }

                                if (oddAnalysis != null && (currentOddValue >= odd_value))
                                {
                                    if (oddAnalysis.EarningPercent >= earningPercente && oddAnalysis.WinningPercent >= winningPercent)
                                    {
                                        odd.OddAnalysis.Add(new OddAnalysisBusinessModel()
                                        {
                                            EarningPercent = oddAnalysis.EarningPercent,
                                            LostCount = oddAnalysis.LostCount,
                                            OddGroupPercent = oddGroupPercent,
                                            WinCount = oddAnalysis.WinCount,
                                            WinningPercent = oddAnalysis.WinningPercent
                                        });

                                        odd.Market = markets.Where(p => p.Id == odd.MarketId).FirstOrDefault();
                                        if (odd.Market != null)
                                        {
                                            odd.Market.Bookmakers = null;
                                        }

                                        fixture.Odds.Add(odd);
                                    }
                                }
                            }
                        });
                    }

                    //foreach (var odd in fixture.Odds)
                    //{
                    //    odd.Market = _marketService.GetMarket(odd.MarketId);
                    //    if (odd.Market != null)
                    //    {
                    //        odd.Market.Bookmarkers = null;
                    //    }
                    //}

                    if (fixture.Odds != null && fixture.Odds.Count > 0)
                    {
                        fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                        fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                        if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                        {
                            continue;
                        }
                        AddTimeZone(fixture, zone);
                        hotRateFixtures.Add(fixture);
                    }
                }

                oddAnalysisBaseModel.Fixture = hotRateFixtures;
                oddAnalysisBaseModel.Success = true;
                return oddAnalysisBaseModel;
            }
            catch (Exception)
            {
                oddAnalysisBaseModel.IsLastPage = false;
                oddAnalysisBaseModel.Success = false;
                return oddAnalysisBaseModel;
            }
        }

        public FixtureForOddAnalysisBaseBusinessModel GetWinningPercenteFixtures(string date, long bookmakerId, long marketId, int winningPercent, string part, string rate, int allFixture, int page, string timeZone)
        {
            FixtureForOddAnalysisBaseBusinessModel oddAnalysisBaseModel = new FixtureForOddAnalysisBaseBusinessModel();
            oddAnalysisBaseModel.Page = page;

            try
            {
                double zone;
                if (!double.TryParse(timeZone, out zone))
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                    oddAnalysisBaseModel.Success = true;
                    return oddAnalysisBaseModel;
                }

                DateTime currentUTCDate = DateTime.UtcNow;
                DateTime currentLocalDateTime = currentUTCDate.AddMinutes(zone);
                DateTime atDate = DateTime.Parse(date);

                double localZoneSeconds = zone * 60;
                DateTime utcDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                int utcTimestamp = (int)(atDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double localNowTimestamp = (int)(currentLocalDateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double utcNowTimestamp = (int)(currentUTCDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double startTimestamp = utcTimestamp - localZoneSeconds;
                double endTimestamp = (startTimestamp + 86400);

                int odd_value = GetValueFromAnalysisRatePart(Enum.Parse<AnalysisRatePart>(rate));
                int count = GetValueFromAnalysisPart(Enum.Parse<AnalysisPart>(part));

                List<FixtureForOddAnalysisBusinessModel> fixtures;
                if (allFixture == 0)
                {
                    fixtures = _mapper.Map<List<FixtureForOddAnalysisBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= utcNowTimestamp && p.startingAtTimestamp >= startTimestamp && p.startingAtTimestamp < endTimestamp).OrderBy(p => p.startingAtTimestamp).Skip(page * takeFixture).Take(takeFixture));
                }
                else
                {
                    fixtures = _mapper.Map<List<FixtureForOddAnalysisBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= startTimestamp && p.startingAtTimestamp < endTimestamp).OrderBy(p => p.startingAtTimestamp).Skip(page * takeFixture).Take(takeFixture));
                }

                List<FixtureForOddAnalysisBusinessModel> winningPercenteFixtures = new List<FixtureForOddAnalysisBusinessModel>();

                var oddAnalysisList = _oddService.GetWinningPercenteOdds(DateTime.Parse(date).AddDays(-1).ToString("yyyy-MM-dd"), bookmakerId, marketId, winningPercent, count, odd_value);
                if (oddAnalysisList.Count == 0)
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                    oddAnalysisBaseModel.Success = true;
                    return oddAnalysisBaseModel;
                }

                if (fixtures.Count < takeFixture)
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                }
                else
                {
                    oddAnalysisBaseModel.IsLastPage = false;
                }

                var leagueList = (List<LeagueBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Leagues_Live, date, "_live"));
                var countryList = (List<CountryBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Countries_Live, date, "_live"));
                List<MarketBusinessModel> markets = _marketService.GetMarkets();
                foreach (var fixture in fixtures.OrderBy(p => p.TimeStartingAtTimestamp))
                {
                    if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                    {
                        continue;
                    }

                    LeagueBusinessModel league = null;
                    if (leagueList != null)
                    {
                        league = leagueList.SingleOrDefault(p => p.Id == fixture.LeagueId);
                        if (league == null)
                        {
                            league = _leagueService.GetLeague(fixture.LeagueId);
                            if (league == null)
                            {
                                continue;
                            }
                            else
                            {
                                fixture.League = league;
                            }
                        }
                        else
                        {
                            fixture.League = league;
                        }
                    }
                    else
                    {
                        league = _leagueService.GetLeague(fixture.LeagueId);
                        if (league == null)
                        {
                            continue;
                        }
                        else
                        {
                            fixture.League = league;
                        }
                    }

                    CountryBusinessModel country = null;
                    if (countryList != null)
                    {
                        country = countryList.FirstOrDefault(p => p.Id == fixture.League.CountryId); //_countryService.GetCountry(league.CountryId);
                        if (country == null)
                        {
                            country = _countryService.GetCountry(fixture.League.CountryId);
                            if (country == null)
                            {
                                continue;
                            }
                            else
                            {
                                fixture.Country = country;
                            }
                        }
                        else
                        {
                            fixture.Country = country;
                        }
                    }
                    else
                    {
                        country = _countryService.GetCountry(fixture.League.CountryId);
                        if (country == null)
                        {
                            continue;
                        }
                        else
                        {
                            fixture.Country = country;
                        }
                    }

                    List<OddBusinessModel> odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixture.Id && p.bookmakerId == bookmakerId && p.marketId == marketId));

                    if (odds.Count > 0)
                    {
                        Parallel.ForEach(odds, odd =>
                        {
                            if (odd.OddValue != "0")
                            {
                                decimal oddGroupPercent = CalculateOddGroupPercent(odd, odds);
                                HotRateBusinessModel oddAnalysis = oddAnalysisList.Where(p => p.OddLabel == odd.OddLabel && p.OddTotal == (string.IsNullOrEmpty(odd.OddTotal) ? null : odd.OddTotal) && p.OddValue == odd.OddValue && p.OddHandicap == (string.IsNullOrEmpty(odd.OddHandicap) ? null : odd.OddHandicap)).OrderBy(p => p.Id).FirstOrDefault();
                                int currentOddValue = int.Parse(odd.OddValue.Replace(".", ""));
                                if (odd.OddValue.Length == 1)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "00");
                                }
                                else if (odd.OddValue.Length == 2)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "00");
                                }
                                else if (odd.OddValue.Length == 3)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "0");
                                }

                                if (oddAnalysis != null && (currentOddValue >= odd_value))
                                {
                                    if (oddAnalysis.WinningPercent >= winningPercent)
                                    {
                                        odd.OddAnalysis.Add(new OddAnalysisBusinessModel()
                                        {
                                            EarningPercent = oddAnalysis.EarningPercent,
                                            LostCount = oddAnalysis.LostCount,
                                            OddGroupPercent = oddGroupPercent,
                                            WinCount = oddAnalysis.WinCount,
                                            WinningPercent = oddAnalysis.WinningPercent
                                        });

                                        odd.Market = markets.Where(p => p.Id == odd.MarketId).FirstOrDefault();
                                        if (odd.Market != null)
                                        {
                                            odd.Market.Bookmakers = null;
                                        }

                                        fixture.Odds.Add(odd);
                                    }
                                }
                            }
                        });
                    }
                    //foreach (var odd in fixture.Odds)
                    //{
                    //    odd.Market = _marketService.GetMarket(odd.MarketId);
                    //    if (odd.Market != null)
                    //    {
                    //        odd.Market.Bookmarkers = null;
                    //    }
                    //}

                    if (fixture.Odds != null && fixture.Odds.Count > 0)
                    {
                        fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                        fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                        if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                        {
                            continue;
                        }
                        AddTimeZone(fixture, zone);
                        winningPercenteFixtures.Add(fixture);
                    }
                }

                oddAnalysisBaseModel.Fixture = winningPercenteFixtures;
                oddAnalysisBaseModel.Success = true;
                return oddAnalysisBaseModel;
            }
            catch (Exception exc)
            {
                oddAnalysisBaseModel.IsLastPage = false;
                oddAnalysisBaseModel.Success = false;
                return oddAnalysisBaseModel;
            }
        }

        public FixtureForOddAnalysisBaseBusinessModel GetEarningPercenteFixtures(string date, long bookmakerId, long marketId, string part, string rate, int allFixture, int page, string timeZone)
        {
            FixtureForOddAnalysisBaseBusinessModel oddAnalysisBaseModel = new FixtureForOddAnalysisBaseBusinessModel();
            oddAnalysisBaseModel.Page = page;

            try
            {
                double zone;
                if (!double.TryParse(timeZone, out zone))
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                    oddAnalysisBaseModel.Success = true;
                    return oddAnalysisBaseModel;
                }

                DateTime currentUTCDate = DateTime.UtcNow;
                DateTime currentLocalDateTime = currentUTCDate.AddMinutes(zone);
                DateTime atDate = DateTime.Parse(date);

                double localZoneSeconds = zone * 60;
                DateTime utcDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                int utcTimestamp = (int)(atDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double localNowTimestamp = (int)(currentLocalDateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double utcNowTimestamp = (int)(currentUTCDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                double startTimestamp = utcTimestamp - localZoneSeconds;
                double endTimestamp = (startTimestamp + 86400);

                int odd_value = GetValueFromAnalysisRatePart(Enum.Parse<AnalysisRatePart>(rate));
                int count = GetValueFromAnalysisPart(Enum.Parse<AnalysisPart>(part));

                List<FixtureForOddAnalysisBusinessModel> fixtures;
                if (allFixture == 0)
                {
                    fixtures = _mapper.Map<List<FixtureForOddAnalysisBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= utcNowTimestamp && p.startingAtTimestamp >= startTimestamp && p.startingAtTimestamp < endTimestamp).OrderBy(p => p.startingAtTimestamp).Skip(page * takeFixture).Take(takeFixture));
                }
                else
                {
                    fixtures = _mapper.Map<List<FixtureForOddAnalysisBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.startingAtTimestamp >= startTimestamp && p.startingAtTimestamp < endTimestamp).OrderBy(p => p.startingAtTimestamp).Skip(page * takeFixture).Take(takeFixture));
                }

                List<FixtureForOddAnalysisBusinessModel> earningPercenteFixtures = new List<FixtureForOddAnalysisBusinessModel>();

                var oddAnalysisList = _oddService.GetEarningPercenteOdds(DateTime.Parse(date).AddDays(-1).ToString("yyyy-MM-dd"), bookmakerId, marketId, count, odd_value);
                if (oddAnalysisList.Count == 0)
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                    oddAnalysisBaseModel.Success = true;
                    return oddAnalysisBaseModel;
                }

                if (fixtures.Count < takeFixture)
                {
                    oddAnalysisBaseModel.IsLastPage = true;
                }
                else
                {
                    oddAnalysisBaseModel.IsLastPage = false;
                }

                var leagueList = (List<LeagueBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Leagues_Live, date, "_live"));
                var countryList = (List<CountryBusinessModel>)_cacheHelper.Get(string.Format(CacheKeys.Countries_Live, date, "_live"));

                List<MarketBusinessModel> markets = _marketService.GetMarkets();
                foreach (var fixture in fixtures.OrderBy(p => p.TimeStartingAtTimestamp))
                {
                    if (fixture.TimeStatus == "AU" || fixture.TimeStatus == "Deleted")
                    {
                        continue;
                    }
                    LeagueBusinessModel league = null;
                    if (leagueList != null)
                    {
                        league = leagueList.SingleOrDefault(p => p.Id == fixture.LeagueId);
                        if (league == null)
                        {
                            league = _leagueService.GetLeague(fixture.LeagueId);
                            if (league == null)
                            {
                                continue;
                            }
                            else
                            {
                                fixture.League = league;
                            }
                        }
                        else
                        {
                            fixture.League = league;
                        }
                    }
                    else
                    {
                        league = _leagueService.GetLeague(fixture.LeagueId);
                        if (league == null)
                        {
                            continue;
                        }
                        else
                        {
                            fixture.League = league;
                        }
                    }

                    CountryBusinessModel country = null;
                    if (countryList != null)
                    {
                        country = countryList.FirstOrDefault(p => p.Id == fixture.League.CountryId); //_countryService.GetCountry(league.CountryId);
                        if (country == null)
                        {
                            country = _countryService.GetCountry(fixture.League.CountryId);
                            if (country == null)
                            {
                                continue;
                            }
                            else
                            {
                                fixture.Country = country;
                            }
                        }
                        else
                        {
                            fixture.Country = country;
                        }
                    }
                    else
                    {
                        country = _countryService.GetCountry(fixture.League.CountryId);
                        if (country == null)
                        {
                            continue;
                        }
                        else
                        {
                            fixture.Country = country;
                        }
                    }

                    List<OddBusinessModel> odds = _mapper.Map<List<OddBusinessModel>>(_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == fixture.Id && p.bookmakerId == bookmakerId && p.marketId == marketId));

                    if (odds.Count > 0)
                    {
                        Parallel.ForEach(odds, odd =>
                        {
                            if (odd.OddValue != "0")
                            {
                                decimal oddGroupPercent = CalculateOddGroupPercent(odd, odds);
                                HotRateBusinessModel oddAnalysis = oddAnalysisList.Where(p => p.OddLabel == odd.OddLabel && p.OddTotal == (string.IsNullOrEmpty(odd.OddTotal) ? null : odd.OddTotal) && p.OddValue == odd.OddValue && p.OddHandicap == (string.IsNullOrEmpty(odd.OddHandicap) ? null : odd.OddHandicap)).OrderBy(p => p.Id).FirstOrDefault();
                                int currentOddValue = int.Parse(odd.OddValue.Replace(".", ""));

                                if (odd.OddValue.Length == 1)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "00");
                                }
                                else if (odd.OddValue.Length == 2)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "00");
                                }
                                else if (odd.OddValue.Length == 3)
                                {
                                    currentOddValue = int.Parse(odd.OddValue.Replace(".", "") + "0");
                                }

                                if (oddAnalysis != null && (currentOddValue >= odd_value))
                                {
                                    if (oddAnalysis.EarningPercent >= 0)
                                    {
                                        odd.OddAnalysis.Add(new OddAnalysisBusinessModel()
                                        {
                                            EarningPercent = oddAnalysis.EarningPercent,
                                            LostCount = oddAnalysis.LostCount,
                                            OddGroupPercent = oddGroupPercent,
                                            WinCount = oddAnalysis.WinCount,
                                            WinningPercent = oddAnalysis.WinningPercent
                                        });

                                        odd.Market = markets.Where(p => p.Id == odd.MarketId).FirstOrDefault();
                                        if (odd.Market != null)
                                        {
                                            odd.Market.Bookmakers = null;
                                        }

                                        fixture.Odds.Add(odd);
                                    }
                                }
                            }
                        });
                    }
                    //foreach (var odd in fixture.Odds)
                    //{
                    //    odd.Market = _marketService.GetMarket(odd.MarketId);
                    //    if (odd.Market != null)
                    //    {
                    //        odd.Market.Bookmarkers = null;
                    //    }
                    //}

                    if (fixture.Odds != null && fixture.Odds.Count > 0)
                    {
                        fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                        fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                        if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                        {
                            continue;
                        }
                        AddTimeZone(fixture, zone);
                        earningPercenteFixtures.Add(fixture);
                    }
                }

                oddAnalysisBaseModel.Fixture = earningPercenteFixtures;
                oddAnalysisBaseModel.Success = true;
                return oddAnalysisBaseModel;
            }
            catch (Exception)
            {
                oddAnalysisBaseModel.IsLastPage = false;
                oddAnalysisBaseModel.Success = false;
                return oddAnalysisBaseModel;
            }
        }

        private List<FixtureForLeagueBusinessModel> GetFixtureForLastMatches(FixtureBusinessModel fixtureModel, int timeStamp, long teamId, string timeZone, bool isLocalTeam, int take)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new List<FixtureForLeagueBusinessModel>();
            }

            //&& p.time_status != "AU" && p.time_status != "Deleted"
            List<FixtureForLeagueBusinessModel> fixtureListModel = new List<FixtureForLeagueBusinessModel>();
            List<FixtureForLeagueBusinessModel> fixtureList;
            if (isLocalTeam)
            {
                fixtureList = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.localTeamId == teamId && p.startingAtTimestamp < timeStamp)).OrderByDescending(p => p.TimeStartingAtDate).Take(take).ToList();
            }
            else
            {
                fixtureList = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => p.visitorTeamId == teamId && p.startingAtTimestamp < timeStamp)).OrderByDescending(p => p.TimeStartingAtDate).Take(take).ToList();
            }

            StatisticAnalysisBusinessModel teamStatisticAnalysisModel = new StatisticAnalysisBusinessModel();
            foreach (var fixture in fixtureList)
            {
                AddTimeZone(fixture, zone);
                List<StatisticBusinessModel> teamStats = _statisticService.GetStatistics(fixture.Id, teamId);
                foreach (var teamStat in teamStats)
                {
                    //teamStatisticAnalysisModel.AttacksAttacks += teamStats.AttacksAttacks;
                    //teamStatisticAnalysisModel.BallSafe += teamStats.BallSafe;
                    //teamStatisticAnalysisModel.Corners += teamStats.Corners;
                    //teamStatisticAnalysisModel.Fouls += teamStats.Fouls;
                    //teamStatisticAnalysisModel.FreeKick += teamStats.FreeKick;
                    //teamStatisticAnalysisModel.GoalAttempts += teamStats.GoalAttempts;
                    //teamStatisticAnalysisModel.GoalKick += teamStats.GoalKick;
                    //teamStatisticAnalysisModel.Offsides += teamStats.Offsides;
                    //teamStatisticAnalysisModel.PassesAccurate += teamStats.PassesAccurate;
                    //teamStatisticAnalysisModel.PassesPercentage += teamStats.PassesPercentage;
                    //teamStatisticAnalysisModel.PassesTotal += teamStats.PassesTotal;
                    //teamStatisticAnalysisModel.PossessionTime += teamStats.PossessionTime;
                    //teamStatisticAnalysisModel.RedCards += teamStats.RedCards;
                    //teamStatisticAnalysisModel.Saves += teamStats.Saves;
                    //teamStatisticAnalysisModel.ShotsBlocked += teamStats.ShotsBlocked;
                    //teamStatisticAnalysisModel.ShotsInsideBox += teamStats.ShotsInsideBox;
                    //teamStatisticAnalysisModel.ShotsOffGoal += teamStats.ShotsOffGoal;
                    //teamStatisticAnalysisModel.ShotsOnGoal += teamStats.ShotsOnGoal;
                    //teamStatisticAnalysisModel.ShotsOutsideBox += teamStats.ShotsOutsideBox;
                    //teamStatisticAnalysisModel.ShotsTotal += teamStats.ShotsTotal;
                    //teamStatisticAnalysisModel.Substitutions += teamStats.Substitutions;
                    //teamStatisticAnalysisModel.ThrowIn += teamStats.ThrowIn;
                    //teamStatisticAnalysisModel.YellowCards += teamStats.YellowCards;
                }

                int localTeamHTScore, visitorTeamHTScore;

                if (fixture.HtScore != null)
                {
                    if (Int32.TryParse(fixture.HtScore.Split('-')[0].Trim(), out localTeamHTScore))
                    {
                        if (Int32.TryParse(fixture.HtScore.Split('-')[1].Trim(), out visitorTeamHTScore))
                        {
                            if (isLocalTeam)
                            {
                                teamStatisticAnalysisModel.GoalHTFor += localTeamHTScore;
                                teamStatisticAnalysisModel.GoalHTAgainst += visitorTeamHTScore;

                            }
                            else
                            {
                                teamStatisticAnalysisModel.GoalHTFor += visitorTeamHTScore;
                                teamStatisticAnalysisModel.GoalHTAgainst += localTeamHTScore;
                            }
                            teamStatisticAnalysisModel.GoalHTTotal += localTeamHTScore + visitorTeamHTScore;
                        }
                    }
                }

                if (isLocalTeam)
                {
                    teamStatisticAnalysisModel.GoalFor += fixture.LocalTeamScore;
                    teamStatisticAnalysisModel.GoalAgainst += fixture.VisitorTeamScore;

                }
                else
                {
                    teamStatisticAnalysisModel.GoalFor += fixture.VisitorTeamScore;
                    teamStatisticAnalysisModel.GoalAgainst += fixture.LocalTeamScore;
                }
                teamStatisticAnalysisModel.GoalTotal += fixture.LocalTeamScore + fixture.VisitorTeamScore;

                fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                {
                    continue;
                }


                fixtureListModel.Add(fixture);
            }

            teamStatisticAnalysisModel.AttacksAttacks = teamStatisticAnalysisModel.AttacksAttacks > 0 ? Math.Round((teamStatisticAnalysisModel.AttacksAttacks / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.BallSafe = teamStatisticAnalysisModel.BallSafe > 0 ? Math.Round((teamStatisticAnalysisModel.BallSafe / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.Corners = teamStatisticAnalysisModel.Corners > 0 ? Math.Round((teamStatisticAnalysisModel.Corners / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.Fouls = teamStatisticAnalysisModel.Fouls > 0 ? Math.Round((teamStatisticAnalysisModel.Fouls / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.FreeKick = teamStatisticAnalysisModel.FreeKick > 0 ? Math.Round((teamStatisticAnalysisModel.FreeKick / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalAttempts = teamStatisticAnalysisModel.GoalAttempts > 0 ? Math.Round((teamStatisticAnalysisModel.GoalAttempts / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalKick = teamStatisticAnalysisModel.GoalKick > 0 ? Math.Round((teamStatisticAnalysisModel.GoalKick / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.Offsides = teamStatisticAnalysisModel.Offsides > 0 ? Math.Round((teamStatisticAnalysisModel.Offsides / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.PassesAccurate = teamStatisticAnalysisModel.PassesAccurate > 0 ? Math.Round((teamStatisticAnalysisModel.PassesAccurate / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.PassesPercentage = teamStatisticAnalysisModel.PassesPercentage > 0 ? Math.Round((teamStatisticAnalysisModel.PassesPercentage / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.PassesTotal = teamStatisticAnalysisModel.PassesTotal > 0 ? Math.Round((teamStatisticAnalysisModel.PassesTotal / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.PossessionTime = teamStatisticAnalysisModel.PossessionTime > 0 ? Math.Round((teamStatisticAnalysisModel.PossessionTime / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.RedCards = teamStatisticAnalysisModel.RedCards > 0 ? Math.Round((teamStatisticAnalysisModel.RedCards / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.Saves = teamStatisticAnalysisModel.Saves > 0 ? Math.Round((teamStatisticAnalysisModel.Saves / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.ShotsBlocked = teamStatisticAnalysisModel.ShotsBlocked > 0 ? Math.Round((teamStatisticAnalysisModel.ShotsBlocked / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.ShotsInsideBox = teamStatisticAnalysisModel.ShotsInsideBox > 0 ? Math.Round((teamStatisticAnalysisModel.ShotsInsideBox / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.ShotsOffGoal = teamStatisticAnalysisModel.ShotsOffGoal > 0 ? Math.Round((teamStatisticAnalysisModel.ShotsOffGoal / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.ShotsOnGoal = teamStatisticAnalysisModel.ShotsOnGoal > 0 ? Math.Round((teamStatisticAnalysisModel.ShotsOnGoal / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.ShotsOutsideBox = teamStatisticAnalysisModel.ShotsOutsideBox > 0 ? Math.Round((teamStatisticAnalysisModel.ShotsOutsideBox / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.ShotsTotal = teamStatisticAnalysisModel.ShotsTotal > 0 ? Math.Round((teamStatisticAnalysisModel.ShotsTotal / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.Substitutions = teamStatisticAnalysisModel.Substitutions > 0 ? Math.Round((teamStatisticAnalysisModel.Substitutions / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.ThrowIn = teamStatisticAnalysisModel.ThrowIn > 0 ? Math.Round((teamStatisticAnalysisModel.ThrowIn / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.YellowCards = teamStatisticAnalysisModel.YellowCards > 0 ? Math.Round((teamStatisticAnalysisModel.YellowCards / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalFor = teamStatisticAnalysisModel.GoalFor > 0 ? Math.Round((teamStatisticAnalysisModel.GoalFor / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalAgainst = teamStatisticAnalysisModel.GoalAgainst > 0 ? Math.Round((teamStatisticAnalysisModel.GoalAgainst / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalTotal = teamStatisticAnalysisModel.GoalTotal > 0 ? Math.Round((teamStatisticAnalysisModel.GoalTotal / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalHTFor = teamStatisticAnalysisModel.GoalHTFor > 0 ? Math.Round((teamStatisticAnalysisModel.GoalHTFor / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalHTAgainst = teamStatisticAnalysisModel.GoalHTAgainst > 0 ? Math.Round((teamStatisticAnalysisModel.GoalHTAgainst / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.GoalHTTotal = teamStatisticAnalysisModel.GoalHTTotal > 0 ? Math.Round((teamStatisticAnalysisModel.GoalHTTotal / fixtureList.Count), 2) : 0;
            teamStatisticAnalysisModel.TotalMatchCount = fixtureList.Count;

            if (isLocalTeam)
            {
                fixtureModel.LocalTeamStatisticAnalysis = teamStatisticAnalysisModel;
            }
            else
            {
                fixtureModel.VisitorTeamStatisticAnalysis = teamStatisticAnalysisModel;
            }
            return fixtureListModel;
        }

        private List<FixtureForLeagueBusinessModel> GetFixtureForHeadToHeadLastMatches(string date, long localTeamId, long visitorTeamId, string timeZone, int take)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new List<FixtureForLeagueBusinessModel>();
            }

            DateTime fixtureDate;
            if (!DateTime.TryParse(date, out fixtureDate))
            {
                return new List<FixtureForLeagueBusinessModel>();
            }

            //&& p.time_status != "AU" && p.time_status != "Deleted"
            List<FixtureForLeagueBusinessModel> fixtureListModel = new List<FixtureForLeagueBusinessModel>();
            var fixtureList = _mapper.Map<List<FixtureForLeagueBusinessModel>>(_unitOfWork.Repository<fixture>().GetList(p => (p.localTeamId == localTeamId || p.visitorTeamId == localTeamId) && (p.localTeamId == visitorTeamId || p.visitorTeamId == visitorTeamId) && p.startingAt < fixtureDate)).OrderByDescending(p => p.TimeStartingAtDate).Take(take).ToList();
            foreach (var fixture in fixtureList)
            {
                AddTimeZone(fixture, zone);
                fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
                fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
                if (fixture.LocalTeam == null || fixture.VisitorTeam == null)
                {
                    continue;
                }
                fixtureListModel.Add(fixture);
            }

            return fixtureListModel;
        }

        private void SetFixtureFullInformations(FixtureBusinessModel fixture, string timeZone)
        {
            fixture.League = _leagueService.GetLeague(fixture.LeagueId);
            fixture.Country = _countryService.GetCountry(fixture.League.CountryId);

            if (fixture.LocalTeamCoachId != 0)
            {
                fixture.LocalTeamCoach = _coachService.GetCoach(fixture.LocalTeamCoachId);
            }
            if (fixture.VisitorTeamCoachId != 0)
            {
                fixture.VisitorTeamCoach = _coachService.GetCoach(fixture.VisitorTeamCoachId);
            }

            fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
            fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
            fixture.Round = _roundService.GetRound(fixture.RoundId);
            fixture.Stage = _stageService.GetStage(fixture.StageId);
            fixture.Season = _seasonService.GetSeason(fixture.SeasonId);
            fixture.Group = _groupService.GetGroup(fixture.GroupId);

            fixture.LocalTeamLineup = _lineupService.GetLineups(fixture.Id, fixture.LocalTeamId);
            fixture.VisitorTeamLineup = _lineupService.GetLineups(fixture.Id, fixture.VisitorTeamId);
            fixture.LocalTeamBench = _benchService.GetBenchs(fixture.Id, fixture.LocalTeamId);
            fixture.VisitorTeamBench = _benchService.GetBenchs(fixture.Id, fixture.VisitorTeamId);
            //fixture.LocalTeamCorner = _cornerService.GetCorners(fixture.Id, fixture.LocalTeamId);
            //fixture.VisitorTeamCorner = _cornerService.GetCorners(fixture.Id, fixture.VisitorTeamId);
            //fixture.LocalTeamSidelined = _sidelinedService.GetSidelineds(fixture.Id, fixture.LocalTeamId);
            //fixture.VisitorTeamSidelined = _sidelinedService.GetSidelineds(fixture.Id, fixture.VisitorTeamId);
            fixture.LocalTeamStatistic = _statisticService.GetStatistics(fixture.Id, fixture.LocalTeamId);
            fixture.VisitorTeamStatistic = _statisticService.GetStatistics(fixture.Id, fixture.VisitorTeamId);
            fixture.TeamsStatistics = _statisticService.GetStatistics(fixture.Id);
            //fixture.SeasonStats = _statisticService.GetSeasonStats(fixture.LeagueId, fixture.SeasonId);
            fixture.LocalTeamEvents = _eventsService.GetEvents(fixture.Id, fixture.LocalTeamId);
            fixture.VisitorTeamEvents = _eventsService.GetEvents(fixture.Id, fixture.VisitorTeamId);
            fixture.TeamsEvents = _eventsService.GetEvents(fixture.Id, fixture.LocalTeamId,fixture.VisitorTeamId);

            //fixture.Comment = _commentService.GetComments(fixture.Id);
            //fixture.Highlight = _highlightService.GetHighlights(fixture.Id);
            //fixture.Referee = _refereeService.GetReferee(fixture.RefereeId);
            //fixture.Tvstation = _tvstationService.GetTvstations(fixture.Id);
            //fixture.Venue = _venueService.GetVenue(fixture.VenueId);
            fixture.Standing = _standingService.GetStandings(fixture.LeagueId, fixture.SeasonId, fixture.StageId, fixture.GroupId);
            //var localTeamForm = fixture.Standing.Where(p => p.StandingsTeamId == fixture.LocalTeamId).FirstOrDefault();
            //if (localTeamForm != null)
            //{
            //    fixture.LocalTeamForm = localTeamForm.StandingsRecentForm;
            //}
            //var visitorTeamForm = fixture.Standing.Where(p => p.StandingsTeamId == fixture.VisitorTeamId).FirstOrDefault();
            //if (visitorTeamForm != null)
            //{
            //    fixture.VisitorTeamForm = visitorTeamForm.StandingsRecentForm;
            //}

            fixture.LocalTeamLastMatches = GetFixtureForLastMatches(fixture, fixture.TimeStartingAtTimestamp, fixture.LocalTeamId, timeZone, true, 10);
            fixture.VisitorTeamLastMatches = GetFixtureForLastMatches(fixture, fixture.TimeStartingAtTimestamp, fixture.VisitorTeamId, timeZone, false, 10);
            fixture.HeadToHeadLastMatches = GetFixtureForHeadToHeadLastMatches(fixture.TimeStartingAtDate, fixture.LocalTeamId, fixture.VisitorTeamId, timeZone, 10);
        }

        private void SetFixtureInformationsForLeague(FixtureForLeagueBusinessModel fixture)
        {
            fixture.LocalTeam = _teamService.GetTeam(fixture.LocalTeamId);
            fixture.VisitorTeam = _teamService.GetTeam(fixture.VisitorTeamId);
        }

        private void AddTimeZone(FixtureBusinessModel fixture, double timeZone)
        {
            DateTime addedFixtureDate = DateTime.Parse(fixture.TimeStartingAtDateTime);
            addedFixtureDate = addedFixtureDate.AddMinutes(timeZone);

            fixture.TimeStartingAtDate = addedFixtureDate.ToString("yyyy-MM-dd");
            fixture.TimeStartingAtDateTime = addedFixtureDate.ToString("yyyy-MM-dd HH:mm:ss");
            fixture.TimeStartingAtTime = addedFixtureDate.ToString("HH:mm:ss");
        }

        private void AddTimeZone(fixture fixture, double timeZone)
        {
            DateTime addedFixtureDate = fixture.startingAt.Value;
            addedFixtureDate = addedFixtureDate.AddMinutes(timeZone);

            fixture.startingAt = addedFixtureDate;
            //fixture.time_starting_at_date_time = addedFixtureDate.ToString("yyyy-MM-dd HH:mm:ss");
            //fixture.time_starting_at_time = addedFixtureDate.ToString("HH:mm:ss");
        }

        private void AddTimeZone(FixtureForLeagueBusinessModel fixture, double timeZone)
        {
            DateTime addedFixtureDate = DateTime.Parse(fixture.TimeStartingAtDateTime);
            addedFixtureDate = addedFixtureDate.AddMinutes(timeZone);

            fixture.TimeStartingAtDate = addedFixtureDate.ToString("yyyy-MM-dd");
            fixture.TimeStartingAtDateTime = addedFixtureDate.ToString("yyyy-MM-dd HH:mm:ss");
            fixture.TimeStartingAtTime = addedFixtureDate.ToString("HH:mm:ss");
        }

        private void AddTimeZone(FixtureDetailHeaderBusinessModel fixture, double timeZone)
        {
            DateTime addedFixtureDate = DateTime.Parse(fixture.TimeStartingAtDateTime);
            addedFixtureDate = addedFixtureDate.AddMinutes(timeZone);

            fixture.TimeStartingAtDate = addedFixtureDate.ToString("yyyy-MM-dd");
            fixture.TimeStartingAtDateTime = addedFixtureDate.ToString("yyyy-MM-dd HH:mm:ss");
            fixture.TimeStartingAtTime = addedFixtureDate.ToString("HH:mm:ss");
        }

        private void AddTimeZone(FixtureForDateBusinessModel fixture, double timeZone)
        {
            DateTime addedFixtureDate = DateTime.Parse(fixture.TimeStartingAtDateTime);
            addedFixtureDate = addedFixtureDate.AddMinutes(timeZone);

            fixture.TimeStartingAtDate = addedFixtureDate.ToString("yyyy-MM-dd");
            fixture.TimeStartingAtDateTime = addedFixtureDate.ToString("yyyy-MM-dd HH:mm:ss");
            fixture.TimeStartingAtTime = addedFixtureDate.ToString("HH:mm:ss");
        }

        private void AddTimeZone(FixtureForOddAnalysisBusinessModel fixture, double timeZone)
        {
            DateTime addedFixtureDate = DateTime.Parse(fixture.TimeStartingAtDateTime);
            addedFixtureDate = addedFixtureDate.AddMinutes(timeZone);

            fixture.TimeStartingAtDate = addedFixtureDate.ToString("yyyy-MM-dd");
            fixture.TimeStartingAtDateTime = addedFixtureDate.ToString("yyyy-MM-dd HH:mm:ss");
            fixture.TimeStartingAtTime = addedFixtureDate.ToString("HH:mm:ss");
        }

        private DailyTimeZoneVO calculateTimezone(string date, int tarihSecim, int statu, double zone)
        {
            DateTime currentUTCDate = DateTime.UtcNow;
            DateTime currentLocalDateTime = currentUTCDate.AddMinutes(zone);
            DateTime atDate = DateTime.Parse(date);
            if (tarihSecim == 0)
            {
                atDate = currentUTCDate.Date;
                if (currentUTCDate.Hour < 4)
                {
                    atDate = atDate.AddDays(-1);
                }
            }

            double localZoneSeconds = zone * 60;
            DateTime utcDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            int utcTimestamp = (int)(atDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            double utcNowTimestamp = (int)(currentUTCDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            double startTimestamp = utcTimestamp;

            double endTimestamp = 0;
            if (tarihSecim == 0)
            {
                endTimestamp = (startTimestamp + 108000);
            }
            else
            {
                startTimestamp = utcTimestamp - localZoneSeconds;
                endTimestamp = (startTimestamp + 86400);
            }

            return new DailyTimeZoneVO()
            {
                startTimestamp = startTimestamp,
                endTimestamp = endTimestamp
            };
        }

        private decimal CalculateOddGroupPercent(OddBusinessModel odd)
        {
            if (odd.OddValue == "0")
            {
                return 0;
            }

            var odds = _unitOfWork.Repository<odd>().GetList(p => p.fixtureId == odd.FixtureId && p.bookmakerId == odd.BookmakerId && p.marketId == odd.MarketId && p.total == odd.OddTotal && p.handicap == odd.OddHandicap);
            decimal karPayi = 0;
            foreach (var item in odds)
            {
                if (item.value == "0")
                {
                    continue;
                }
                else
                {
                    karPayi += Convert.ToDecimal(1 / decimal.Parse(item.value));
                }
            }

            decimal dagitilacakTutar = 1 / karPayi;

            decimal dagitilacakTutarOrani = 100 * dagitilacakTutar;

            return Math.Round(Convert.ToDecimal(Convert.ToDecimal(1 / decimal.Parse(odd.OddValue)) * dagitilacakTutarOrani), 2);
        }

        private decimal CalculateOddGroupPercent(OddBusinessModel odd, List<OddBusinessModel> oddList)
        {
            if (odd.OddValue == "0")
            {
                return 0;
            }

            var odds = oddList.Where(p => p.OddTotal == odd.OddTotal && p.OddHandicap == odd.OddHandicap); //_unitOfWork.Repository<odd>().GetList(p => p.fixtureId == odd.FixtureId && p.bookmakerId == odd.BookmarkerId && p.marketId == odd.MarketId && p.odd_total == odd.OddTotal && p.odd_handicap == odd.OddHandicap);
            decimal karPayi = 0;
            foreach (var item in odds)
            {
                if (item.OddValue == "0")
                {
                    continue;
                }
                else
                {
                    karPayi += Convert.ToDecimal(1 / decimal.Parse(item.OddValue));
                }
            }

            decimal dagitilacakTutar = 1 / karPayi;

            decimal dagitilacakTutarOrani = 100 * dagitilacakTutar;

            return Math.Round(Convert.ToDecimal(Convert.ToDecimal(1 / decimal.Parse(odd.OddValue)) * dagitilacakTutarOrani), 2);
        }

        private int GetValueFromAnalysisRatePart(AnalysisRatePart rate)
        {
            switch (rate)
            {
                case AnalysisRatePart.Rate_1: return 100;
                case AnalysisRatePart.Rate_110: return 110;
                case AnalysisRatePart.Rate_125: return 125;
                case AnalysisRatePart.Rate_150: return 150;
                case AnalysisRatePart.Rate_200: return 200;
                case AnalysisRatePart.Rate_500: return 500;
                case AnalysisRatePart.Rate_1000: return 1000;
                default: return 100;
            }
        }

        private int GetValueFromAnalysisPart(AnalysisPart part)
        {
            switch (part)
            {
                case AnalysisPart.OneMonth: return 1;
                case AnalysisPart.ThreeMonth: return 3;
                case AnalysisPart.SixMonth: return 6;
                case AnalysisPart.OneYear: return 12;
                case AnalysisPart.All: return 0;
                default: return 1;
            }
        }
    }
}
