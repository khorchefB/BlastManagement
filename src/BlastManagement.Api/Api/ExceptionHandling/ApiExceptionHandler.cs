using BlastManagement.Api.Application.Exceptions;
using BlastManagement.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BlastManagement.Api.Api.ExceptionHandling;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            DomainRuleViolationException =>
                (StatusCodes.Status400BadRequest, "Domain rule violation", exception.Message),

            BlastNotFoundException or HoleNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found", exception.Message),

            OptimisticConcurrencyException =>
                (StatusCodes.Status409Conflict, "Concurrency conflict", exception.Message),

            _ =>
                (StatusCodes.Status500InternalServerError,
                    "Unexpected server error",
                    "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing the request.");
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path.Value
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
