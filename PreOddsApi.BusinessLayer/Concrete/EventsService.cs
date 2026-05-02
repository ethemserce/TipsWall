using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System.Collections.Generic;
using System.Linq;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class EventsService : IEventsService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IPlayerService _playerService;
        private readonly IMapper _mapper;

        public EventsService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IPlayerService playerService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _playerService = playerService;
            _mapper = mapper;
        }

        public List<EventsBusinessModel> GetEvents(long fixtureId, long teamId)
        {
            var events = _mapper.Map<List<EventsBusinessModel>>(_unitOfWork.Repository<events>().GetList(p => p.fixtureId == fixtureId && p.teamId == teamId).OrderBy(x=>x.minute).ToList());
            //var events = _unitOfWork.Repository<events>().GetList(p => p.fixtureId == fixtureId && p.teamId == teamId);
 

            if (events != null)
            {
                if (events.Count() > 0)
                {
                    var types = _unitOfWork.Repository<types>().GetList().ToList();
                    foreach (var item in events)
                    {
                        var type = types.FirstOrDefault(x => x.id == item.TypeId);
                        item.Type = type.code;
                        item.TypeName = type.name;
                    }
                }
            }

            return events;
        }

        public List<EventsBusinessModel> GetEvents(long fixtureId, long localTeamId, long visitorTeamId)
        {
            var events = _mapper.Map<List<EventsBusinessModel>>(_unitOfWork.Repository<events>().GetList(p => p.fixtureId == fixtureId));

            if (events != null)
            {
                var types = _unitOfWork.Repository<types>().GetList().ToList();
                foreach (var item in events)
                {
                    var type = types.FirstOrDefault(x => x.id == item.TypeId);
                    item.Type = type.code;
                    item.TypeName = type.name;
                    if (item.TeamId == localTeamId)
                    {
                        item.Position = 0;
                    }
                    else if (item.TeamId == visitorTeamId)
                    {
                        item.Position = 1;
                    }
                }

                return events.OrderBy(x => x.Minute).ToList();
            }
            else
            {
                return new List<EventsBusinessModel>();
            }
        }
    }
}
