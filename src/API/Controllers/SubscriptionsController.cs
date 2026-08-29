using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IGenericRepository<Subscription> _subscriptionRepository;

    public SubscriptionsController(IGenericRepository<Subscription> subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubscriptions()
    {
        var tenantId = GetCurrentTenantId();
        var subs = await _subscriptionRepository.FindAsync(s => s.TenantId == tenantId);
        var result = subs.OrderByDescending(s => s.CreatedAt).Select(s => new SubscriptionDto
        {
            Id = s.Id, PackageName = s.PackageName, SubscriptionFrom = s.SubscriptionFrom,
            SubscriptionTo = s.SubscriptionTo, InvoiceNo = s.InvoiceNo, InvoiceDate = s.InvoiceDate,
            PaymentModeId = s.PaymentModeId, Remark = s.Remark, IsActive = s.IsActive, CreatedAt = s.CreatedAt
        }).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSubscription(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var subs = await _subscriptionRepository.FindAsync(s => s.Id == id && s.TenantId == tenantId);
        var sub = subs.FirstOrDefault();
        if (sub == null) return NotFound();
        return Ok(new SubscriptionDto
        {
            Id = sub.Id, PackageName = sub.PackageName, SubscriptionFrom = sub.SubscriptionFrom,
            SubscriptionTo = sub.SubscriptionTo, InvoiceNo = sub.InvoiceNo, InvoiceDate = sub.InvoiceDate,
            PaymentModeId = sub.PaymentModeId, Remark = sub.Remark, IsActive = sub.IsActive, CreatedAt = sub.CreatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Tenant context required" });

        var sub = new Subscription
        {
            Id = Guid.NewGuid(), TenantId = tenantId, PackageName = request.PackageName,
            SubscriptionFrom = request.SubscriptionFrom, SubscriptionTo = request.SubscriptionTo,
            InvoiceNo = request.InvoiceNo, InvoiceDate = request.InvoiceDate,
            PaymentModeId = request.PaymentModeId, Remark = request.Remark,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        await _subscriptionRepository.AddAsync(sub);
        return CreatedAtAction(nameof(GetSubscription), new { id = sub.Id }, new { id = sub.Id, message = "Subscription created" });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateSubscriptionRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var subs = await _subscriptionRepository.FindAsync(s => s.Id == id && s.TenantId == tenantId);
        var sub = subs.FirstOrDefault();
        if (sub == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.PackageName)) sub.PackageName = request.PackageName;
        if (request.SubscriptionFrom.HasValue) sub.SubscriptionFrom = request.SubscriptionFrom.Value;
        if (request.SubscriptionTo.HasValue) sub.SubscriptionTo = request.SubscriptionTo.Value;
        if (!string.IsNullOrWhiteSpace(request.InvoiceNo)) sub.InvoiceNo = request.InvoiceNo;
        if (request.InvoiceDate.HasValue) sub.InvoiceDate = request.InvoiceDate.Value;
        if (request.PaymentModeId.HasValue) sub.PaymentModeId = request.PaymentModeId;
        if (request.Remark != null) sub.Remark = request.Remark;

        await _subscriptionRepository.UpdateAsync(sub);
        return Ok(new { message = "Subscription updated" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var subs = await _subscriptionRepository.FindAsync(s => s.Id == id && s.TenantId == tenantId);
        var sub = subs.FirstOrDefault();
        if (sub == null) return NotFound();

        sub.IsActive = false;
        await _subscriptionRepository.UpdateAsync(sub);
        return Ok(new { message = "Subscription deactivated" });
    }
}

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public string PackageName { get; set; } = "";
    public DateOnly SubscriptionFrom { get; set; }
    public DateOnly SubscriptionTo { get; set; }
    public string InvoiceNo { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public Guid? PaymentModeId { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSubscriptionRequest
{
    public string PackageName { get; set; } = "";
    public DateOnly SubscriptionFrom { get; set; }
    public DateOnly SubscriptionTo { get; set; }
    public string InvoiceNo { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public Guid? PaymentModeId { get; set; }
    public string? Remark { get; set; }
}

public class UpdateSubscriptionRequest
{
    public string? PackageName { get; set; }
    public DateOnly? SubscriptionFrom { get; set; }
    public DateOnly? SubscriptionTo { get; set; }
    public string? InvoiceNo { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public Guid? PaymentModeId { get; set; }
    public string? Remark { get; set; }
}
