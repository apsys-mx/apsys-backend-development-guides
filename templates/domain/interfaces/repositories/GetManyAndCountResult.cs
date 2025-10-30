namespace {ProjectName}.domain.interfaces.repositories;

public class GetManyAndCountResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Count { get; set; }
}
