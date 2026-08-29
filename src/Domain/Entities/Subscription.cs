namespace FMS.Domain.Entities;

/// <summary>
/// Tenant subscription tracking with invoices and payments.
/// </summary>
public class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public DateOnly SubscriptionFrom { get; set; }
    public DateOnly SubscriptionTo { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public Guid? PaymentModeId { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
