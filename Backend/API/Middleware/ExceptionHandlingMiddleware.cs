using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WhatsAppParser.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failed for {Path}: {Errors}",
                context.Request.Path,
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));

            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                "Validation Error",
                "https://httpstatuses.io/400",
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = ex.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => (object)g.Select(e => e.ErrorMessage).ToArray())
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);

            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "https://httpstatuses.io/500");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string type,
        Dictionary<string, object?>? extensions = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Instance = context.Request.Path
        };

        if (extensions is not null)
            foreach (var (key, value) in extensions)
                problem.Extensions[key] = value;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
