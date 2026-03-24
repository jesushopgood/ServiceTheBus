using System.Net;
using System.Text.Json;
using FluentValidation;

namespace OrderService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = ex is ValidationException
                ? (int)HttpStatusCode.BadRequest
                : (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = ex is ValidationException validationException
                ? JsonSerializer.Serialize(new
                {
                    error = "Validation failed.",
                    errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }),
                    traceId = context.TraceIdentifier
                })
                : JsonSerializer.Serialize(new
                {
                    error = "An unexpected error occurred.",
                    traceId = context.TraceIdentifier
                });

            await context.Response.WriteAsync(payload);
        }
    }
}

