using FluentValidation;
using FluentValidation.Results;

namespace MiProyecto.domain.entities;

/// <summary>
/// Clase base abstracta para objetos de dominio.
/// </summary>
public abstract class AbstractDomainObject
{
    /// <summary>
    /// Constructor
    /// </summary>
    protected AbstractDomainObject()
    {
        this.CreationDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id"></param>
    /// <param name="creationDate"></param>
    protected AbstractDomainObject(Guid id, DateTime creationDate)
    {
        Id = id;
        CreationDate = creationDate;
    }

    /// <summary>
    /// Gets or sets the unique identifier for the domain object.
    /// This identifier is automatically generated if not provided.
    /// </summary>
    public virtual Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the creation date of the domain object.
    /// This property is automatically set to the current date and time when the object is created.
    /// </summary>
    public virtual DateTime CreationDate { get; set; }

    /// <summary>
    /// Validates the current instance of the domain object.
    /// This method uses FluentValidation to check if the object meets its validation rules.
    /// </summary>
    /// <returns></returns>
    public virtual bool IsValid()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return true;

        var context = new ValidationContext<object>(this);
        ValidationResult result = validator.Validate(context);
        return result.IsValid;
    }

    /// <summary>
    /// Validates the current instance of the domain object and returns any validation failures.
    /// This method uses FluentValidation to check if the object meets its validation rules and returns a collection of validation failures if any exist.
    /// If no validator is defined, it returns an empty list of validation failures.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<ValidationFailure> Validate()
    {
        IValidator? validator = GetValidator();
        if (validator == null)
            return new List<ValidationFailure>();
        else
        {
            var context = new ValidationContext<object>(this);
            ValidationResult result = validator.Validate(context);
            return result.Errors;
        }
    }

    /// <summary>
    /// Gets the validator for the domain object.
    /// This method should be overridden in derived classes to provide a specific validator for the entity.
    /// </summary>
    /// <returns></returns>
    public virtual IValidator? GetValidator()
         => null;
}
