namespace {ProjectName}.domain.exceptions;

/// <summary>
/// Exception that is thrown when an invalid argument is provided for a filter operation.
/// This exception is typically used to indicate that a filter argument does not meet the expected criteria
/// </summary>
public class InvalidFilterArgumentException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterArgumentException"/> class
    /// with a default error message.
    /// </summary>
    public InvalidFilterArgumentException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterArgumentException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public InvalidFilterArgumentException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFilterArgumentException"/> class
    /// with a specified error message and parameter name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="param"></param>
    public InvalidFilterArgumentException(string message, string param) : base(message, param) { }
}
