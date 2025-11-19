using AutoMapper;
using MiProyecto.domain.interfaces.repositories;
using MiProyecto.webapi.dtos;

namespace MiProyecto.webapi.mappingprofiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapeo genérico de GetManyAndCountResult<T> a GetManyAndCountResultDto<T>
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
    }
}
