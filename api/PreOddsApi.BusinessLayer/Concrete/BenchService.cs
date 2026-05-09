using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class BenchService : IBenchService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IPlayerService _playerService;
        private readonly IMapper _mapper;

        public BenchService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IPlayerService playerService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _playerService = playerService;
            _mapper = mapper;
        }

        public List<LineupBusinessModel> GetBenchs(long fixtureId, long teamId)
        {
            //return _mapper.Map<List<BenchBusinessModel>>(_unitOfWork.Repository<bench>().GetList(p => p.fixtureId == fixtureId && p.team_id == teamId));
            var bench = _unitOfWork.Repository<lineup>().GetList(p => p.fixtureId == fixtureId && p.teamId == teamId && p.formationPosition == null);
            if (bench != null)
            {
                if (bench.Count() > 0)
                {
                    var benchs = _mapper.Map<List<LineupBusinessModel>>(bench);
                    foreach (var item in benchs)
                    {
                        item.Player = _playerService.GetPlayer(item.PlayerId);
                    }
                    return benchs;
                }
            }

            return new List<LineupBusinessModel>();
        }
    }
}
