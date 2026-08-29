using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Route = FMS.Domain.Entities.Route;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/routes")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly IGenericRepository<Route> _routeRepository;

    public RoutesController(IGenericRepository<Route> routeRepository)
    {
        _routeRepository = routeRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutes(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null)
    {
        var tenantId = GetCurrentTenantId();
        var allRoutes = await _routeRepository.FindAsync(r => r.TenantId == tenantId);
        var query = allRoutes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.RouteName.Contains(search) || r.StartLocation.Contains(search) || r.EndLocation.Contains(search));

        var totalCount = query.Count();
        var routes = query.OrderBy(r => r.RouteName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new RouteDto
            {
                Id = r.Id, RouteName = r.RouteName, StartLocation = r.StartLocation, EndLocation = r.EndLocation,
                StartLatitude = r.StartLatitude, StartLongitude = r.StartLongitude,
                EndLatitude = r.EndLatitude, EndLongitude = r.EndLongitude,
                Waypoints = r.Waypoints, RouteTypeId = r.RouteTypeId,
                DistanceKm = r.DistanceKm, EstimatedDurationMin = r.EstimatedDurationMin,
                IsActive = r.IsActive, CreatedAt = r.CreatedAt
            }).ToList();

        return Ok(new { items = routes, totalCount, pageNumber = page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRoute(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var routes = await _routeRepository.FindAsync(r => r.Id == id && r.TenantId == tenantId);
        var route = routes.FirstOrDefault();
        if (route == null) return NotFound();
        return Ok(new RouteDto
        {
            Id = route.Id, RouteName = route.RouteName, StartLocation = route.StartLocation, EndLocation = route.EndLocation,
            StartLatitude = route.StartLatitude, StartLongitude = route.StartLongitude,
            EndLatitude = route.EndLatitude, EndLongitude = route.EndLongitude,
            Waypoints = route.Waypoints, RouteTypeId = route.RouteTypeId,
            DistanceKm = route.DistanceKm, EstimatedDurationMin = route.EstimatedDurationMin,
            IsActive = route.IsActive, CreatedAt = route.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Tenant context required" });

        var route = new Domain.Entities.Route
        {
            Id = Guid.NewGuid(), TenantId = tenantId, RouteName = request.RouteName,
            StartLocation = request.StartLocation, EndLocation = request.EndLocation,
            StartLatitude = request.StartLatitude, StartLongitude = request.StartLongitude,
            EndLatitude = request.EndLatitude, EndLongitude = request.EndLongitude,
            Waypoints = request.Waypoints ?? new(), RouteTypeId = request.RouteTypeId,
            DistanceKm = request.DistanceKm, EstimatedDurationMin = request.EstimatedDurationMin,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        await _routeRepository.AddAsync(route);
        return CreatedAtAction(nameof(GetRoute), new { id = route.Id }, new { id = route.Id, message = "Route created" });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRoute(Guid id, [FromBody] UpdateRouteRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var routes = await _routeRepository.FindAsync(r => r.Id == id && r.TenantId == tenantId);
        var route = routes.FirstOrDefault();
        if (route == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.RouteName)) route.RouteName = request.RouteName;
        if (!string.IsNullOrWhiteSpace(request.StartLocation)) route.StartLocation = request.StartLocation;
        if (!string.IsNullOrWhiteSpace(request.EndLocation)) route.EndLocation = request.EndLocation;
        if (request.StartLatitude.HasValue) route.StartLatitude = request.StartLatitude;
        if (request.StartLongitude.HasValue) route.StartLongitude = request.StartLongitude;
        if (request.EndLatitude.HasValue) route.EndLatitude = request.EndLatitude;
        if (request.EndLongitude.HasValue) route.EndLongitude = request.EndLongitude;
        if (request.Waypoints != null) route.Waypoints = request.Waypoints;
        if (request.RouteTypeId.HasValue) route.RouteTypeId = request.RouteTypeId;
        if (request.DistanceKm.HasValue) route.DistanceKm = request.DistanceKm;
        if (request.EstimatedDurationMin.HasValue) route.EstimatedDurationMin = request.EstimatedDurationMin;
        if (request.IsActive.HasValue) route.IsActive = request.IsActive.Value;

        route.UpdatedAt = DateTime.UtcNow;
        await _routeRepository.UpdateAsync(route);
        return Ok(new { message = "Route updated" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRoute(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var routes = await _routeRepository.FindAsync(r => r.Id == id && r.TenantId == tenantId);
        var route = routes.FirstOrDefault();
        if (route == null) return NotFound();

        await _routeRepository.DeleteAsync(route);
        return Ok(new { message = "Route deleted" });
    }
}

public class RouteDto
{
    public Guid Id { get; set; }
    public string RouteName { get; set; } = "";
    public string StartLocation { get; set; } = "";
    public string EndLocation { get; set; } = "";
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public List<Dictionary<string, object>> Waypoints { get; set; } = new();
    public Guid? RouteTypeId { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRouteRequest
{
    public string RouteName { get; set; } = "";
    public string StartLocation { get; set; } = "";
    public string EndLocation { get; set; } = "";
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public List<Dictionary<string, object>>? Waypoints { get; set; }
    public Guid? RouteTypeId { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? EstimatedDurationMin { get; set; }
}

public class UpdateRouteRequest
{
    public string? RouteName { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public List<Dictionary<string, object>>? Waypoints { get; set; }
    public Guid? RouteTypeId { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public bool? IsActive { get; set; }
}
