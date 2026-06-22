using CourierMax.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using FluentValidationException = FluentValidation.ValidationException;

namespace CourierMax.Api.Middleware;

/// <summary>
/// Manejo centralizado de errores. Traduce las excepciones de validacion y de
/// dominio a respuestas ProblemDetails con el codigo HTTP adecuado, y registra
/// cualquier error no controlado como 500 sin filtrar detalles internos.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidationException ex)
        {
            await WriteValidationProblem(context, ex);
        }
        catch (DomainException ex)
        {
            await WriteDomainProblem(context, ex);
        }
        catch (ArgumentException ex)
        {
            // Invariantes violadas en los value objects: entrada incorrecta.
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Solicitud invalida", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado procesando {Path}.", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Error interno", "Ocurrio un error inesperado. Intente nuevamente mas tarde.");
        }
    }

    private static Task WriteValidationProblem(HttpContext context, FluentValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Uno o mas errores de validacion",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };

        return WriteResponse(context, StatusCodes.Status400BadRequest, problem);
    }

    private Task WriteDomainProblem(HttpContext context, DomainException ex)
    {
        var status = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            BusinessRuleException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        _logger.LogWarning("Regla de negocio: {Message} ({Status}).", ex.Message, status);
        return WriteProblem(context, status, TitleFor(status), ex.Message);
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "Recurso no encontrado",
        StatusCodes.Status409Conflict => "Conflicto con el estado actual",
        _ => "Solicitud invalida"
    };

    private static Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
        return WriteResponse(context, status, problem);
    }

    private static Task WriteResponse(HttpContext context, int status, ProblemDetails problem)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(problem, problem.GetType());
    }
}
