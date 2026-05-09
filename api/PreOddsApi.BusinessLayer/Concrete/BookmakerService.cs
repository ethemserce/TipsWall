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
    public class BookmakerService : IBookmakerService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public BookmakerService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public BookmakerBusinessModel GetBookmaker(long bookmarkerId)
        {
            return _mapper.Map<BookmakerBusinessModel>(_unitOfWork.Repository<bookmaker>().Get(p => p.id == bookmarkerId));
        }

        public List<BookmakerBusinessModel> GetBookmaker()
        {
            return _mapper.Map<List<BookmakerBusinessModel>>(_unitOfWork.Repository<bookmaker>().GetList());
        }
    }
}
