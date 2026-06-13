using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LittleHelpers.ApiService.Application.Cqrs;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            RequestAuthenticationException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            RequestAuthorizationException => (StatusCodes.Status403Forbidden, "Forbidden"),
            RequestNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            RequestValidationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => default
        };

        if (statusCode == 0)
            return false;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
