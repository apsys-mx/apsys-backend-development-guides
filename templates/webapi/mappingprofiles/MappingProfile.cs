using AutoMapper;
using {ProjectName}.domain.interfaces.repositories;
using {ProjectName}.webapi.dtos;

namespace {ProjectName}.webapi.mappingprofiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {

        // Generic mapping from GetManyAndCountResult<T> to GetManyAndCountResultDto<T>
        CreateMap(typeof(GetManyAndCountResult<>), typeof(GetManyAndCountResultDto<>))
            .ForMember(nameof(GetManyAndCountResultDto<object>.SortBy),
                opt => opt.MapFrom((src, _, __, ___) =>
                {
                    return (src as IGetManyAndCountResultWithSorting)?.Sorting.SortBy;
                }))
            .ForMember(nameof(GetManyAndCountResultDto<object>.SortCriteria),
                opt => opt.MapFrom((src, _, __, ___) =>
                {
                    return (src as IGetManyAndCountResultWithSorting)?.Sorting.Criteria switch
                    {
                        SortingCriteriaType.Ascending => "asc",
                        SortingCriteriaType.Descending => "desc",
                        _ => null
                    };
                }));

        // TODO: Add your project-specific mappings here
        // Example:
        // CreateMap<User, UserDto>();
        // CreateMap<Product, ProductDto>();
    }
}
