using AutoMapper;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Core.V3;

namespace SportMonks.Core.Worker.Mapping;

public class CoreMapping : Profile
{
    public CoreMapping()
    {
        AllowNullCollections = true;

        CreateMap<Continent, continent>().ReverseMap();

        CreateMap<Country, country>().ReverseMap();

        CreateMap<Region, region>().ReverseMap();

        CreateMap<City, city>().ReverseMap();

        CreateMap<Types, types>().ReverseMap();
    }
}

internal static class MapperExtention
{
    internal static IMappingExpression<TSource, TDestination> IgnoreAllMembers<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expr)
    {
        var destinationType = typeof(TDestination);

        foreach (var property in destinationType.GetProperties())
            expr.ForMember(property.Name, opt => opt.Ignore());

        return expr;
    }
}