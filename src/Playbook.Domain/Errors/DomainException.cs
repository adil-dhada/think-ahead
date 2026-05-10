namespace Playbook.Domain.Errors;

public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string entity, string id)
    : DomainException("NOT_FOUND", $"{entity} '{id}' was not found.");

public sealed class ConflictException(string code, string message)
    : DomainException(code, message);

public sealed class ForbiddenException(string message)
    : DomainException("FORBIDDEN", message);

public sealed class DomainValidationException(string message)
    : DomainException("DOMAIN_VALIDATION", message);
