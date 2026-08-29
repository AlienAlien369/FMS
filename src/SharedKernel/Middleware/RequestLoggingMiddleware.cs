using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FMS.SharedKernel.Middleware;

/// <summary>
/// Logs incoming requests and outgoing responses with timing, status codes, and tenant context.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        _logger.LogInformation(
            "Incoming {Method} {Path}{QueryString} from {RemoteIP} | UserAgent: {UserAgent}",
            request.Method,
            request.Path,
            request.QueryString,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            request.Headers.UserAgent.ToString().Substring(0, Math.Min(100, request.Headers.UserAgent.ToString().Length)));

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsed = stopwatch.ElapsedMilliseconds;

            var logLevel = statusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel,
                "Completed {Method} {Path} → {StatusCode} in {ElapsedMs}ms",
                request.Method,
                request.Path,
                statusCode,
                elapsed);

            // Track slow requests
            if (elapsed > 1000)
            {
                _logger.LogWarning(
                    "SLOW REQUEST: {Method} {Path} took {ElapsedMs}ms (threshold: 1000ms)",
                    request.Method, request.Path, elapsed);
            }
        }
    }
}
