using FMS.SharedKernel.Models;
using FMS.Telemetry.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Telemetry.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
[Authorize]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryRepository _repository;

    public TelemetryController(ITelemetryRepository repository) => _repository = repository;

    /// <summary>Get device telemetry records.</summary>
    [HttpGet("devices/{deviceId:guid}/records")]
    [ProducesResponseType(typeof(ApiResponse<List<DeviceRecord>>), 200)]
    public async Task<IActionResult> GetDeviceRecords(
        Guid deviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100)
    {
        var tenantId = GetTenantId();
        var effectiveFrom = from ?? DateTime.UtcNow.AddHours(-1);
        var effectiveTo = to ?? DateTime.UtcNow;

        var records = await _repository.GetDeviceRecordsAsync(tenantId, deviceId, effectiveFrom, effectiveTo, limit);
        return Ok(ApiResponse<List<DeviceRecord>>.Ok(records));
    }

    /// <summary>Get latest telemetry for a device.</summary>
    [HttpGet("devices/{deviceId:guid}/latest")]
    [ProducesResponseType(typeof(ApiResponse<DeviceRecord>), 200)]
    public async Task<IActionResult> GetLatestTelemetry(Guid deviceId)
    {
        var tenantId = GetTenantId();
        var record = await _repository.GetLatestRecordAsync(tenantId, deviceId);

        if (record == null)
            return NotFound(ApiResponse<DeviceRecord>.Fail("No telemetry found for this device"));

        return Ok(ApiResponse<DeviceRecord>.Ok(record));
    }

    /// <summary>Get trips for the tenant.</summary>
    [HttpGet("trips")]
    [ProducesResponseType(typeof(ApiResponse<List<TripRecord>>), 200)]
    public async Task<IActionResult> GetTrips([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var tenantId = GetTenantId();
        var trips = await _repository.GetTripsAsync(tenantId, from, to);
        return Ok(ApiResponse<List<TripRecord>>.Ok(trips));
    }

    /// <summary>Get alerts for the tenant.</summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(ApiResponse<List<AlertRecord>>), 200)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] bool unresolvedOnly = false,
        [FromQuery] int limit = 50)
    {
        var tenantId = GetTenantId();
        var alerts = await _repository.GetAlertsAsync(tenantId, unresolvedOnly, limit);
        return Ok(ApiResponse<List<AlertRecord>>.Ok(alerts));
    }

    /// <summary>Resolve an alert.</summary>
    [HttpPut("alerts/{alertId:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid alertId, [FromBody] ResolveAlertRequest request)
    {
        var userId = GetUserId();
        await _repository.ResolveAlertAsync(alertId, userId, request.Notes);
        return Ok(ApiResponse<object>.Ok(null, "Alert resolved"));
    }

    /// <summary>Get dashboard statistics.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStats>), 200)]
    public async Task<IActionResult> GetDashboardStats()
    {
        var tenantId = GetTenantId();
        var stats = await _repository.GetDashboardStatsAsync(tenantId);
        return Ok(ApiResponse<DashboardStats>.Ok(stats));
    }

    private Guid GetTenantId()
    {
        var tenantIdStr = HttpContext.Items["TenantIdString"]?.ToString()
            ?? User.FindFirst("tenant_id")?.Value
            ?? throw new UnauthorizedAccessException("Tenant not resolved");
        return Guid.Parse(tenantIdStr);
    }

    private Guid? GetUserId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userIdStr != null ? Guid.Parse(userIdStr) : null;
    }
}

public class ResolveAlertRequest
{
    public string? Notes { get; set; }
}
