namespace FMS.Domain.Entities;

public class DeviceCommand
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public string CommandType { get; set; } = string.Empty; // immobilize, mobilize, poll, config_update, firmware_update
    public Dictionary<string, object>? Payload { get; set; }
    public string Status { get; set; } = "pending"; // pending, sent, delivered, failed, acknowledged
    public DateTime? SentAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Dictionary<string, object>? ResponsePayload { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Device Device { get; set; } = null!;
    public User? Creator { get; set; }
}
