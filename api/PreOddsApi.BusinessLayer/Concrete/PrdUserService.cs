using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.User;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;


namespace PreOddsApi.BusinessLayer.Concrete
{
    public class PrdUserService : IPrdUserService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public PrdUserService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public PrdUserBusinessModel GetUser(string username, string password)
        {
            return _mapper.Map<PrdUserBusinessModel>(_unitOfWork.Repository<prd_user>().Get(p => p.nick_name == username && p.password == password));
        }

        public PrdUserBusinessModel GetUser(long id)
        {
            return _mapper.Map<PrdUserBusinessModel>(_unitOfWork.Repository<prd_user>().Get(p => p.id == id));
        }
    }
}
