using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System.Collections.Generic;
using System.Linq;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly ICountryService _countryService;
        private readonly IVenueService _venueService;
        private readonly IMapper _mapper;

        public TeamService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, ICountryService countryService, IVenueService venueService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _countryService = countryService;
            _venueService = venueService;
            _mapper = mapper;
        }

        public TeamBusinessModel GetTeam(long teamId)
        {
            var team = _mapper.Map<TeamBusinessModel>(_unitOfWork.Repository<team>().Get(p => p.id == teamId));
            if (team != null && string.IsNullOrEmpty(team.ImagePath))
            {
                team.ImagePath = ""; //"https://im.preodds.com/img/team/default_team.png";
            }
            else
            {
                string s = "";
            }

            return team;
        }

        public TeamBusinessModel GetTeam(long teamId, string lang)
        {
            var team = _mapper.Map<TeamBusinessModel>(_unitOfWork.Repository<team>().Get(p => p.id == teamId));
            if (team != null)
            {
                team.Country = _countryService.GetCountry(team.CountryId);
                team.Venue = _venueService.GetVenue(team.VenueId);
                if (string.IsNullOrEmpty(team.ImagePath))
                {
                    team.ImagePath = ""; //"https://im.preodds.com/img/team/default_team.png";
                }
            }

            return team;
        }

        public TeamBusinessModel ConvertTeamToBusinessModel(team team)
        {
            var teamModel = _mapper.Map<TeamBusinessModel>(team);
            if (teamModel != null)
            {
                if (string.IsNullOrEmpty(teamModel.ImagePath))
                {
                    teamModel.ImagePath = ""; //"https://im.preodds.com/img/team/default_team.png";
                }
            }

            return teamModel;
        }
    }
}
