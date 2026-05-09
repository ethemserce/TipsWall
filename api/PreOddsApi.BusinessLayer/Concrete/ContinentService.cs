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
    public class ContinentService : IContinentService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public ContinentService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public List<ContinentBusinessModel> GetContinents()
        {
            List<ContinentBusinessModel> continentList = new List<ContinentBusinessModel>();
            var query = _unitOfWork.Repository<country>().GetList();
            ContinentBusinessModel internationalContinentModel = _mapper.Map<ContinentBusinessModel>(_unitOfWork.Repository<continent>().Get(p => p.sportmonks_id == 7));
            var countryList = _unitOfWork.Repository<country>().GetList(x => x.continentId == internationalContinentModel.Id);
            foreach (var country in countryList)
            {
                CountryBusinessModel countryModel = _mapper.Map<CountryBusinessModel>(country);
                //country.continent_id = internationalContinentModel.Id;
                internationalContinentModel.Countries.Add(countryModel);
            }
            continentList.Add(internationalContinentModel);

            var queryContinentGroup = query.GroupBy(p => p.continentId);
            foreach (var continentItem in queryContinentGroup.OrderBy(p => p.Key))
            {
                continent continent = _unitOfWork.Repository<continent>().Get(p => p.id == continentItem.Key);
                if (continent != null)
                {
                    ContinentBusinessModel continentModel = new ContinentBusinessModel()
                    {
                        Name = continent.name,
                        CreateDateTime = continent.create_date_time,
                        Id = continent.id,
                        UpdateDateTime = continent.update_date_time
                    };

                    foreach (var country in query.Where(p => p.continentId == continentItem.Key))
                    {
                        CountryBusinessModel countryModel = _mapper.Map<CountryBusinessModel>(country);
                        continentModel.Countries.Add(countryModel);
                    }

                    continentList.Add(continentModel);
                }
            }

            return continentList.ToList();
        }
    }
}
