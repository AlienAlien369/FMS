using System.Security.Claims;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IGenericRepository<Feature> _featureRepository;
    private readonly IGenericRepository<UserPreference> _preferenceRepository;
    private readonly IGenericRepository<User> _userRepository;

    public SettingsController(
        ITenantRepository tenantRepository,
        IGenericRepository<Feature> featureRepository,
        IGenericRepository<UserPreference> preferenceRepository,
        IGenericRepository<User> userRepository)
    {
        _tenantRepository = tenantRepository;
        _featureRepository = featureRepository;
        _preferenceRepository = preferenceRepository;
        _userRepository = userRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    // ── Tenant Settings ──
    [HttpGet("tenant")]
    public async Task<IActionResult> GetTenantSettings()
    {
        var tenantId = GetCurrentTenantId();
        var tenants = await _tenantRepository.FindAsync(t => t.Id == tenantId);
        var tenant = tenants.FirstOrDefault();
        if (tenant == null) return NotFound();

        return Ok(new TenantSettingsDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            CountryCode = tenant.CountryCode,
            Timezone = tenant.Timezone,
            Currency = tenant.Currency,
            Plan = tenant.Plan,
            Status = tenant.Status,
            DataResidencyRegion = tenant.DataResidencyRegion,
            Settings = tenant.Settings,
            CreatedAt = tenant.CreatedAt
        });
    }

    [HttpPut("tenant")]
    public async Task<IActionResult> UpdateTenantSettings([FromBody] UpdateTenantSettingsRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var tenants = await _tenantRepository.FindAsync(t => t.Id == tenantId);
        var tenant = tenants.FirstOrDefault();
        if (tenant == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name)) tenant.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Timezone)) tenant.Timezone = request.Timezone;
        if (!string.IsNullOrWhiteSpace(request.Currency)) tenant.Currency = request.Currency;
        if (request.Settings != null) tenant.Settings = request.Settings;

        tenant.UpdatedAt = DateTime.UtcNow;
        await _tenantRepository.UpdateAsync(tenant);
        return Ok(new { message = "Tenant settings updated" });
    }

    // ── Features ──
    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures()
    {
        var tenantId = GetCurrentTenantId();
        var features = await _featureRepository.FindAsync(f => f.TenantId == tenantId);
        return Ok(features.Select(f => new FeatureDto
        {
            Id = f.Id,
            Module = f.Module,
            FeatureName = f.FeatureName,
            Enabled = f.Enabled,
            Config = f.Config
        }));
    }

    [HttpPut("features/{id:guid}")]
    public async Task<IActionResult> UpdateFeature(Guid id, [FromBody] UpdateFeatureRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var features = await _featureRepository.FindAsync(f => f.Id == id && f.TenantId == tenantId);
        var feature = features.FirstOrDefault();
        if (feature == null) return NotFound();

        feature.Enabled = request.Enabled;
        if (request.Config != null) feature.Config = request.Config;

        await _featureRepository.UpdateAsync(feature);
        return Ok(new { message = "Feature updated" });
    }

    // ── User Preferences ──
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var uid)) return BadRequest();

        var prefs = await _preferenceRepository.FindAsync(p => p.UserId == uid);
        return Ok(prefs.Select(p => new PreferenceDto
        {
            Id = p.Id,
            Page = p.Page,
            PreferenceType = p.PreferenceType,
            Config = p.Config,
            UpdatedAt = p.UpdatedAt
        }));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> SavePreference([FromBody] SavePreferenceRequest request)
    {
        var userId = User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var uid)) return BadRequest();

        var existing = await _preferenceRepository.FindAsync(p => p.UserId == uid && p.Page == request.PageName && p.PreferenceType == request.PreferenceType);
        var pref = existing.FirstOrDefault();

        if (pref == null)
        {
            pref = new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = uid,
                Page = request.PageName,
                PreferenceType = request.PreferenceType,
                Config = request.Config ?? new(),
                UpdatedAt = DateTime.UtcNow
            };
            await _preferenceRepository.AddAsync(pref);
        }
        else
        {
            pref.Config = request.Config ?? pref.Config;
            pref.UpdatedAt = DateTime.UtcNow;
            await _preferenceRepository.UpdateAsync(pref);
        }

        return Ok(new { message = "Preference saved" });
    }

    // ── System Stats (admin) ──
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = GetCurrentTenantId();
        var users = await _userRepository.FindAsync(u => u.TenantId == tenantId);
        var features = await _featureRepository.FindAsync(f => f.TenantId == tenantId);

        return Ok(new
        {
            totalUsers = users.Count,
            activeUsers = users.Count(u => u.IsActive),
            mfaEnabled = users.Count(u => u.MfaEnabled),
            totalFeatures = features.Count,
            enabledFeatures = features.Count(f => f.Enabled),
            disabledFeatures = features.Count(f => !f.Enabled)
        });
    }
}

// DTOs
public class TenantSettingsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Subdomain { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string Timezone { get; set; } = "";
    public string Currency { get; set; } = "";
    public string Plan { get; set; } = "";
    public string Status { get; set; } = "";
    public string? DataResidencyRegion { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class UpdateTenantSettingsRequest
{
    public string? Name { get; set; }
    public string? Timezone { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}

public class FeatureDto
{
    public Guid Id { get; set; }
    public string Module { get; set; } = "";
    public string FeatureName { get; set; } = "";
    public bool Enabled { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
}

public class UpdateFeatureRequest
{
    public bool Enabled { get; set; }
    public Dictionary<string, object>? Config { get; set; }
}

public class PreferenceDto
{
    public Guid Id { get; set; }
    public string Page { get; set; } = "";
    public string PreferenceType { get; set; } = "";
    public Dictionary<string, object> Config { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class SavePreferenceRequest
{
    public string PageName { get; set; } = "";
    public string PreferenceType { get; set; } = "";
    public Dictionary<string, object>? Config { get; set; }
}
