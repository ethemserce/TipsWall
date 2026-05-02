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
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public GroupService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public GroupBusinessModel GetGroup(long? groupId)
        {
            if(groupId == null)
            {
                return new GroupBusinessModel();
            }
            return _mapper.Map<GroupBusinessModel>(_unitOfWork.Repository<group>().Get(p => p.id == groupId));
        }

        public GroupBusinessModel GetGroup(long roundId, long stageId)
        {
            return _mapper.Map<GroupBusinessModel>(_unitOfWork.Repository<group>().Get(p => p.stageId == stageId));
        }

        public List<GroupBusinessModel> GetGroups(long stageId)
        {
            return _mapper.Map<List<GroupBusinessModel>>(_unitOfWork.Repository<group>().GetList(p => p.stageId == stageId)).OrderByDescending(p=>p.Id).ToList();
        }

    }
}
