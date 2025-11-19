namespace MiProyecto.infrastructure.nhibernate.filtering;

public class InvalidQueryStringArgumentException : Exception
{
    public InvalidQueryStringArgumentException(string argName)
        : base($"Parameter [{argName}] has an invalid value.")
    {
    }
}
