using FMS.Config.Data;
using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace FMS.Config.Services;

public interface IConfigService
{
    Task<List<FeatureDto>> GetFeaturesAsync(Guid tenantId);
    Task ToggleFeatureAsync(Guid tenantId, string module, string featureName, bool enabled);
    Task<List<NavigationModuleDto>> GetNavigationAsync(Guid tenantId, List<string> permissions);
    Task<List<UserPreferenceDto>> GetUserPreferencesAsync(Guid userId);
    Task SaveUserPreferenceAsync(Guid userId, string page, string preferenceType, Dictionary<string, object> config);
    Task<BrandingDto> GetBrandingAsync(Guid tenantId);
    Task UpdateBrandingAsync(Guid tenantId, BrandingDto branding);
}

public class ConfigService : IConfigService
{
    private readonly ConfigDbContext _db;
    private readonly IDistributedCache? _cache;
    private readonly ILogger<ConfigService> _logger;

    public ConfigService(ConfigDbContext db, ILogger<ConfigService> logger, IDistributedCache? cache = null)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<FeatureDto>> GetFeaturesAsync(Guid tenantId)
    {
        return await _db.Features
            .Where(f => f.TenantId == tenantId)
            .Select(f => new FeatureDto(f.Module, f.FeatureName, f.Enabled, f.Config))
            .ToListAsync();
    }

    public async Task ToggleFeatureAsync(Guid tenantId, string module, string featureName, bool enabled)
    {
        var feature = await _db.Features.FirstOrDefaultAsync(f =>
            f.TenantId == tenantId && f.Module == module && f.FeatureName == featureName);

        if (feature == null)
        {
            feature = new Feature
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Module = module,
                FeatureName = featureName,
                Enabled = enabled
            };
            _db.Features.Add(feature);
        }
        else
        {
            feature.Enabled = enabled;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Feature {Module}/{FeatureName} {State} for tenant {TenantId}",
            module, featureName, enabled ? "enabled" : "disabled", tenantId);
    }

    public async Task<List<NavigationModuleDto>> GetNavigationAsync(Guid tenantId, List<string> permissions)
    {
        // Get enabled features for tenant
        var enabledFeatures = await _db.Features
            .Where(f => f.TenantId == tenantId && f.Enabled)
            .Select(f => new { f.Module, f.FeatureName })
            .ToListAsync();

        var enabledSet = new HashSet<string>(enabledFeatures.Select(f => $"{f.Module}/{f.FeatureName}"));

        // Filter navigation by enabled features and user permissions
        var allModules = GetDefaultNavigation();
        var filteredModules = new List<NavigationModuleDto>();

        foreach (var module in allModules)
        {
            var filteredItems = module.Items
                .Where(item => enabledSet.Contains($"{module.Id}/{item.Id}"))
                .Where(item => item.RequiredPermissions.All(p => permissions.Contains(p) || permissions.Contains("*")))
                .ToList();

            if (filteredItems.Any())
            {
                filteredModules.Add(new NavigationModuleDto(module.Id, module.Label, module.Icon, filteredItems));
            }
        }

        return filteredModules;
    }

    public async Task<List<UserPreferenceDto>> GetUserPreferencesAsync(Guid userId)
    {
        return await _db.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => new UserPreferenceDto(p.Page, p.PreferenceType, p.Config, p.UpdatedAt))
            .ToListAsync();
    }

    public async Task SaveUserPreferenceAsync(Guid userId, string page, string preferenceType, Dictionary<string, object> config)
    {
        var existing = await _db.UserPreferences.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.Page == page && p.PreferenceType == preferenceType);

        if (existing == null)
        {
            _db.UserPreferences.Add(new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Page = page,
                PreferenceType = preferenceType,
                Config = config,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Config = config;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<BrandingDto> GetBrandingAsync(Guid tenantId)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant == null) return GetDefaultBranding();

        var settings = tenant.Settings;
        if (settings.TryGetValue("branding", out var brandingObj) && brandingObj is JsonElement brandingJson)
        {
            return new BrandingDto
            {
                PrimaryColor = brandingJson.TryGetProperty("primaryColor", out var pc) ? pc.GetString() ?? "#1a73e8" : "#1a73e8",
                SecondaryColor = brandingJson.TryGetProperty("secondaryColor", out var sc) ? sc.GetString() ?? "#3b82f6" : "#3b82f6",
                LogoUrl = brandingJson.TryGetProperty("logoUrl", out var logo) ? logo.GetString() : "/assets/logo.svg",
                CompanyName = tenant.Name,
                IsRtl = settings.TryGetValue("rtl", out var rtlObj) && rtlObj is JsonElement rtlJson && rtlJson.GetBoolean()
            };
        }

        return GetDefaultBranding();
    }

    public async Task UpdateBrandingAsync(Guid tenantId, BrandingDto branding)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant == null) return;

        tenant.Settings["branding"] = branding;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static BrandingDto GetDefaultBranding() => new()
    {
        PrimaryColor = "#1a73e8",
        SecondaryColor = "#3b82f6",
        LogoUrl = "/assets/logo.svg",
        CompanyName = "FMS Fleet Management"
    };

    private static List<NavigationModuleDto> GetDefaultNavigation()
    {
        return new List<NavigationModuleDto>
        {
            new("command-center", "Command Center", "dashboard", new List<NavigationItemDto>
            {
                new("operations-overview", "Operations Overview", "/command-center/operations", "monitor", new() { "command-center:read" }),
                new("live-fleet-map", "Live Fleet Map", "/command-center/fleet-map", "map", new() { "command-center:read" }),
                new("active-alerts", "Active Alerts Hub", "/command-center/alerts", "warning", new() { "command-center:read" })
            }),
            new("fleet-intelligence", "Fleet Intelligence", "directions_car", new List<NavigationItemDto>
            {
                new("vehicle-directory", "Vehicle Directory", "/fleet/vehicles", "local_shipping", new() { "fleet-intelligence:read" }),
                new("driver-hub", "Driver Hub", "/fleet/drivers", "people", new() { "fleet-intelligence:read" }),
                new("maintenance-studio", "Maintenance Studio", "/fleet/maintenance", "build", new() { "fleet-intelligence:read" }),
                new("fuel-analytics", "Fuel & Energy Analytics", "/fleet/fuel", "local_gas_station", new() { "fleet-intelligence:read" }),
                new("geofence-studio", "Geofence Studio", "/fleet/geofences", "fence", new() { "fleet-intelligence:read" })
            }),
            new("trip-logistics", "Trip & Logistics", "route", new List<NavigationItemDto>
            {
                new("trip-planner", "Trip Planner", "/logistics/trips", "add_task", new() { "trip-logistics:read" }),
                new("active-deliveries", "Active Deliveries", "/logistics/deliveries", "local_shipping", new() { "trip-logistics:read" }),
                new("yard-dock", "Yard & Dock Manager", "/logistics/yard", "warehouse", new() { "trip-logistics:read" })
            }),
            new("people-transport", "People & Transport", "groups", new List<NavigationItemDto>
            {
                new("school-bus", "School Bus Console", "/transport/school", "school", new() { "people-transport:read" }),
                new("employee-shuttle", "Employee Shuttle", "/transport/shuttle", "business_center", new() { "people-transport:read" }),
                new("emergency-dispatch", "Emergency Dispatch", "/transport/emergency", "emergency", new() { "people-transport:read" })
            }),
            new("safety-compliance", "Safety & Compliance", "shield", new List<NavigationItemDto>
            {
                new("video-telematics", "Video Telematics", "/safety/video", "videocam", new() { "safety-compliance:read" }),
                new("incident-center", "Incident Center", "/safety/incidents", "report_problem", new() { "safety-compliance:read" }),
                new("document-vault", "Document Vault", "/safety/documents", "folder", new() { "safety-compliance:read" })
            }),
            new("analytics", "Analytics & Insights", "insights", new List<NavigationItemDto>
            {
                new("insight-builder", "Insight Builder", "/analytics/insights", "analytics", new() { "analytics:read" }),
                new("scorecards", "Performance Scorecards", "/analytics/scorecards", "emoji_events", new() { "analytics:read" }),
                new("trip-replay", "Trip Replay Studio", "/analytics/replay", "replay", new() { "analytics:read" })
            }),
            new("settings", "Settings & Config", "settings", new List<NavigationItemDto>
            {
                new("organization", "Organization Hub", "/settings/organization", "business", new() { "settings:read" }),
                new("access-control", "Access Control", "/settings/access", "lock", new() { "settings:read" }),
                new("alert-studio", "Alert Studio", "/settings/alerts", "notifications", new() { "settings:read" }),
                new("brand-theme", "Brand & Theme", "/settings/branding", "palette", new() { "settings:read" })
            }),
            new("device-iot", "Device & IoT", "memory", new List<NavigationItemDto>
            {
                new("device-fleet", "Device Fleet", "/devices/fleet", "router", new() { "device-iot:read" }),
                new("camera-grid", "Camera Grid", "/devices/cameras", "camera", new() { "device-iot:read" }),
                new("device-lab", "Device Lab", "/devices/lab", "science", new() { "device-iot:read" })
            })
        };
    }
}

// ── DTOs ──

public record FeatureDto(string Module, string FeatureName, bool Enabled, Dictionary<string, object> Config);
public record NavigationModuleDto(string Id, string Label, string Icon, List<NavigationItemDto> Items);
public record NavigationItemDto(string Id, string Label, string Route, string Icon, List<string> RequiredPermissions);
public record UserPreferenceDto(string Page, string PreferenceType, Dictionary<string, object> Config, DateTime UpdatedAt);

public class BrandingDto
{
    public string PrimaryColor { get; set; } = "#1a73e8";
    public string SecondaryColor { get; set; } = "#3b82f6";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? FontFamily { get; set; }
    public string CompanyName { get; set; } = "FMS";
    public bool IsRtl { get; set; }
}
