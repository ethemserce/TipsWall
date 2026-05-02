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
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public CommentService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public List<CommentBusinessModel> GetComments(long fixtureId)
        {
            var commentList = _unitOfWork.Repository<comment>().GetList(p => p.fixtureId == fixtureId);
            if (commentList != null)
            {
                return _mapper.Map<List<CommentBusinessModel>>(commentList.OrderByDescending(p => p.order));
            }
            else
            {
                return new List<CommentBusinessModel>();
            }
        }
    }
}
