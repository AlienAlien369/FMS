using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/geofences")]
[Authorize]
public class GeofencesController : ControllerBase
{
    private readonly IGenericRepository<Geofence> _geofenceRepository;

    public GeofencesController(IGenericRepository<Geofence> geofenceRepository)
    {
        _geofenceRepository = geofenceRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetGeofences(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        var tenantId = GetCurrentTenantId();
        var all = await _geofenceRepository.FindAsync(g => g.TenantId == tenantId);
        var query = all.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(g => g.Name.Contains(search) || (g.Address != null && g.Address.Contains(search)));

        var totalCount = query.Count();
        var items = query.OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(g => new GeofenceDto
            {
                Id = g.Id, Name = g.Name, LocationTypeId = g.LocationTypeId, Address = g.Address,
                Latitude = g.Latitude, Longitude = g.Longitude, RadiusMeters = g.RadiusMeters,
                Color = g.Color, IsActive = g.IsActive, CreatedAt = g.CreatedAt
            }).ToList();

        return Ok(new { items = items, totalCount, pageNumber = page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetGeofence(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var items = await _geofenceRepository.FindAsync(g => g.Id == id && g.TenantId == tenantId);
        var item = items.FirstOrDefault();
        if (item == null) return NotFound();
        return Ok(new GeofenceDto
        {
            Id = item.Id, Name = item.Name, LocationTypeId = item.LocationTypeId, Address = item.Address,
            Latitude = item.Latitude, Longitude = item.Longitude, RadiusMeters = item.RadiusMeters,
            Color = item.Color, IsActive = item.IsActive, CreatedAt = item.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateGeofence([FromBody] CreateGeofenceRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Tenant context required" });

        var geofence = new Geofence
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = request.Name,
            LocationTypeId = request.LocationTypeId, Address = request.Address,
            Latitude = request.Latitude, Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters, Color = request.Color ?? "Blue",
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        await _geofenceRepository.AddAsync(geofence);
        return CreatedAtAction(nameof(GetGeofence), new { id = geofence.Id }, new { id = geofence.Id, message = "Geofence created" });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateGeofence(Guid id, [FromBody] UpdateGeofenceRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var items = await _geofenceRepository.FindAsync(g => g.Id == id && g.TenantId == tenantId);
        var geofence = items.FirstOrDefault();
        if (geofence == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name)) geofence.Name = request.Name;
        if (request.LocationTypeId.HasValue) geofence.LocationTypeId = request.LocationTypeId;
        if (request.Address != null) geofence.Address = request.Address;
        if (request.Latitude.HasValue) geofence.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) geofence.Longitude = request.Longitude.Value;
        if (request.RadiusMeters.HasValue) geofence.RadiusMeters = request.RadiusMeters.Value;
        if (request.Color != null) geofence.Color = request.Color;
        if (request.IsActive.HasValue) geofence.IsActive = request.IsActive.Value;

        geofence.UpdatedAt = DateTime.UtcNow;
        await _geofenceRepository.UpdateAsync(geofence);
        return Ok(new { message = "Geofence updated" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteGeofence(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var items = await _geofenceRepository.FindAsync(g => g.Id == id && g.TenantId == tenantId);
        var geofence = items.FirstOrDefault();
        if (geofence == null) return NotFound();

        await _geofenceRepository.DeleteAsync(geofence);
        return Ok(new { message = "Geofence deleted" });
    }
}

public class GeofenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid? LocationTypeId { get; set; }
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal RadiusMeters { get; set; }
    public string Color { get; set; } = "Blue";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGeofenceRequest
{
    public string Name { get; set; } = "";
    public Guid? LocationTypeId { get; set; }
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal RadiusMeters { get; set; }
    public string? Color { get; set; }
}

public class UpdateGeofenceRequest
{
    public string? Name { get; set; }
    public Guid? LocationTypeId { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? RadiusMeters { get; set; }
    public string? Color { get; set; }
    public bool? IsActive { get; set; }
}
