using System.Security.Claims;
using FMS.Application.Common.Interfaces;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMS.API.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = ResolveTenantId(context);
        
        if (tenantId.HasValue)
        {
            // Set tenant context for RLS
            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FmsDbContext>();
            
            await db.Database.ExecuteSqlRawAsync(
                $"SET app.current_tenant = '{tenantId.Value}'");
        }

        // Register current user service
        context.Items["TenantId"] = tenantId;

        await _next(context);
    }

    private Guid? ResolveTenantId(HttpContext context)
    {
        // 1. Check subdomain
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            var subdomain = parts[0];
            // TODO: Look up tenant by subdomain
        }

        // 2. Check header
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue))
        {
            if (Guid.TryParse(headerValue, out var headerTenantId))
                return headerTenantId;
        }

        // 3. Check JWT claim
        var tenantClaim = context.User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var jwtTenantId))
            return jwtTenantId;

        return null;
    }
}
