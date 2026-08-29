using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IGenericRepository<AuditLog> _auditRepository;

    public AuditController(IGenericRepository<AuditLog> auditRepository)
    {
        _auditRepository = auditRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var tenantId = GetCurrentTenantId();
        var all = await _auditRepository.FindAsync(a => a.TenantId == tenantId);
        var query = all.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var totalCount = query.Count();
        var items = query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id, UserId = a.UserId, Action = a.Action, EntityType = a.EntityType,
                EntityId = a.EntityId, OldValue = a.OldValue, NewValue = a.NewValue,
                IpAddress = a.IpAddress, UserAgent = a.UserAgent, CreatedAt = a.CreatedAt
            }).ToList();

        return Ok(new { items, totalCount, pageNumber = page, pageSize });
    }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
