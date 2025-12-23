namespace {ProjectName}.domain.exceptions;

public class InvalidFilterArgumentException : Exception
{
    public InvalidFilterArgumentException(string message) : base(message)
    {
    }

    public InvalidFilterArgumentException(string message, string argName) : base(message)
    {
    }
}
