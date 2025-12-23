namespace MiProyecto.domain.exceptions;

/// <summary>
/// Exception thrown when attempting to create or update an entity that would result in a duplicate.
/// </summary>
public class DuplicatedDomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicatedDomainException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DuplicatedDomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicatedDomainException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DuplicatedDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
