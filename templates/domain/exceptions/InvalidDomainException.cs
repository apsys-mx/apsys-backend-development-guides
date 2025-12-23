using FluentValidation.Results;
using System.Text.Json;

namespace MiProyecto.domain.exceptions;

/// <summary>
/// Invalid domain exception class
/// </summary>
public class InvalidDomainException : Exception
{
    public readonly IEnumerable<ValidationFailure> ValidationFailures;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="validationFailures"></param>
    public InvalidDomainException(IEnumerable<ValidationFailure> validationFailures)
    {
        this.ValidationFailures = validationFailures;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="property"></param>
    /// <param name="errorCode"></param>
    /// <param name="errorMessage"></param>
    public InvalidDomainException(string property, string errorCode, string errorMessage)
    {
        var validationResults = new List<ValidationFailure>
            {
                new()
                {
                    ErrorCode = errorCode,
                    PropertyName = property,
                    ErrorMessage = errorMessage
                }
            };
        this.ValidationFailures = validationResults.AsEnumerable();
    }

    /// <summary>
    /// Get error message
    /// </summary>
    public override string Message
    {
        get
        {
            var messages = from error in this.ValidationFailures
                           select new { error.ErrorMessage, error.ErrorCode, error.PropertyName };
            return JsonSerializer.Serialize(messages);
        }
    }

}
