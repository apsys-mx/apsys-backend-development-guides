using FluentValidation.Results;

namespace {ProjectName}.domain.exceptions;

public class InvalidDomainException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; set; }

    public InvalidDomainException(IEnumerable<ValidationFailure> errors)
        : base("Domain validation failed")
    {
        Errors = errors;
    }
}
