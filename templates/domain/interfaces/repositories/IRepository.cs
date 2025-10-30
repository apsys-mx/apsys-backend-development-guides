namespace {ProjectName}.domain.interfaces.repositories;

public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey> where T : class
{
    T Add(T item);
    Task AddAsync(T item);
    T Save(T item);
    Task SaveAsync(T item);
    void Delete(T item);
    Task DeleteAsync(T item);
}
