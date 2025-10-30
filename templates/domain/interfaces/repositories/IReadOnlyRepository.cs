namespace {ProjectName}.domain.interfaces.repositories;

public interface IReadOnlyRepository<T, TKey> where T : class
{
    T? GetById(TKey id);
    Task<T?> GetByIdAsync(TKey id);
    IEnumerable<T> GetAll();
    Task<IEnumerable<T>> GetAllAsync();
}
