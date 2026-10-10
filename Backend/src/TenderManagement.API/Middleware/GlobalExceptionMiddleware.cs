using System.Net;
using System.Text.Json;
using TenderManagement.Application.Common;

namespace TenderManagement.API.Middleware
{
    /// <summary>
    /// Central exception handler. Converts known Application-layer exceptions into clean
    /// HTTP status codes + JSON error bodies, and logs anything unexpected as a 500.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                BusinessRuleException => (HttpStatusCode.BadRequest, ex.Message),
                ForbiddenActionException => (HttpStatusCode.Forbidden, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            }
            else
            {
                _logger.LogWarning("{StatusCode} on {Path}: {Message}", (int)statusCode, context.Request.Path, message);
            }

            context.Response.StatusCode = (int)statusCode;

            var payload = JsonSerializer.Serialize(new
            {
                success = false,
                statusCode = (int)statusCode,
                message
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
