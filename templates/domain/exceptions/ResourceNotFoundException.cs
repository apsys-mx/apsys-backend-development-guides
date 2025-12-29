namespace {ProjectName}.domain.exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found in the system.
/// </summary>
public class ResourceNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ResourceNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
