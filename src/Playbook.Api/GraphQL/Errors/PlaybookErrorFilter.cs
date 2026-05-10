using FluentValidation;
using HotChocolate;
using Playbook.Domain.Errors;

namespace Playbook.Api.GraphQL.Errors;

public static class PlaybookErrorFilter
{
    public static IError OnError(IError error) => error.Exception switch
    {
        NotFoundException ex => error
            .WithCode("NOT_FOUND")
            .WithMessage(ex.Message)
            .RemoveExtension("stackTrace"),

        ConflictException ex => error
            .WithCode(ex.Code)
            .WithMessage(ex.Message)
            .RemoveExtension("stackTrace"),

        ForbiddenException ex => error
            .WithCode("FORBIDDEN")
            .WithMessage(ex.Message)
            .RemoveExtension("stackTrace"),

        DomainValidationException ex => error
            .WithCode("DOMAIN_VALIDATION")
            .WithMessage(ex.Message)
            .RemoveExtension("stackTrace"),

        ValidationException ex => error
            .WithCode("VALIDATION")
            .WithMessage("Validation failed.")
            .SetExtension("errors", ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }))
            .RemoveExtension("stackTrace"),

        _ => error
    };
}
