using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FMS.SharedKernel.Middleware;

/// <summary>
/// Resolves tenant from subdomain, X-Tenant-ID header, or JWT claim.
/// Sets tenant context for downstream services and RLS.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = ResolveTenantId(context);

        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId.Value;
            context.Items["TenantIdString"] = tenantId.Value.ToString();

            using (LogContext.BeginProperty("TenantId", tenantId.Value))
            {
                _logger.LogDebug("Tenant resolved: {TenantId}", tenantId);
                await _next(context);
            }
        }
        else
        {
            // Allow unauthenticated endpoints (login, health, swagger)
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            var allowsAnonymous = path.StartsWith("/api/v1/auth") ||
                                  path.StartsWith("/health") ||
                                  path.StartsWith("/swagger") ||
                                  path.StartsWith("/api/v1/tenants/onboard");

            if (allowsAnonymous)
            {
                await _next(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "tenant_required",
                    message = "X-Tenant-ID header or tenant_id claim is required"
                });
            }
        }
    }

    private Guid? ResolveTenantId(HttpContext context)
    {
        // 1. Check X-Tenant-ID header (most common for API calls)
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue)
            && Guid.TryParse(headerValue, out var headerTenantId))
        {
            return headerTenantId;
        }

        // 2. Check JWT claim
        var tenantClaim = context.User?.FindFirst("tenant_id");
        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var jwtTenantId))
        {
            return jwtTenantId;
        }

        // 3. Check subdomain (acme-logistics.fms-uat.onrender.com)
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            var subdomain = parts[0];
            // In production, cache this lookup in Redis
            _logger.LogInformation("Subdomain detected: {Subdomain}", subdomain);
            // TODO: Resolve subdomain -> tenantId via Redis cache
        }

        // 4. Check query parameter (for SignalR and debugging)
        if (context.Request.Query.TryGetValue("tenantId", out var queryValue)
            && Guid.TryParse(queryValue, out var queryTenantId))
        {
            return queryTenantId;
        }

        return null;
    }
}

/// <summary>
/// Adds tenant context property to Serilog logging scope.
/// </summary>
internal static class LogContext
{
    public static IDisposable BeginProperty(string key, object value)
    {
        return Serilog.Context.LogContext.PushProperty(key, value);
    }
}
