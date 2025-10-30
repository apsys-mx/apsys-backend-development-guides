namespace {ProjectName}.domain.interfaces.repositories;

public interface IGetManyAndCountResultWithSorting<T> where T : class
{
    GetManyAndCountResult<T> GetManyAndCount(int offset, int limit, string? filter = null, IEnumerable<SortingCriteria>? sorting = null);
    Task<GetManyAndCountResult<T>> GetManyAndCountAsync(int offset, int limit, string? filter = null, IEnumerable<SortingCriteria>? sorting = null);
}
