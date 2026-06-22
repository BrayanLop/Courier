namespace CourierMax.Domain.Exceptions;

/// <summary>
/// Base de las excepciones de regla de negocio. El middleware de la API las
/// traduce a respuestas HTTP en lugar de devolver un 500 generico.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>Se solicito una entidad que no existe (mapea a 404).</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Conflicto con el estado actual del recurso, p.ej. transicion invalida o codigo duplicado (mapea a 409).</summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Violacion de una regla de negocio de entrada (mapea a 400).</summary>
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}
