using System.Security.Claims;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/lookups")]
[Authorize]
public class LookupsController : ControllerBase
{
    private readonly IGenericRepository<Lookup> _lookupRepository;

    public LookupsController(IGenericRepository<Lookup> lookupRepository)
    {
        _lookupRepository = lookupRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetLookups(
        [FromQuery] string? category = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool activeOnly = true)
    {
        var allLookups = await _lookupRepository.GetAllAsync();
        var query = allLookups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(l => l.Category == category);

        if (parentId.HasValue)
            query = query.Where(l => l.ParentId == parentId.Value);

        if (activeOnly)
            query = query.Where(l => l.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Label.Contains(search) || l.Code.Contains(search));

        var result = query.OrderBy(l => l.SortOrder).ThenBy(l => l.Label)
            .Select(l => new LookupDto
            {
                Id = l.Id,
                Category = l.Category,
                ParentId = l.ParentId,
                Code = l.Code,
                Label = l.Label,
                SortOrder = l.SortOrder,
                IsActive = l.IsActive,
                Metadata = l.Metadata,
                CreatedAt = l.CreatedAt
            }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLookup(Guid id)
    {
        var lookups = await _lookupRepository.FindAsync(l => l.Id == id);
        var lookup = lookups.FirstOrDefault();
        if (lookup == null) return NotFound();

        return Ok(new LookupDto
        {
            Id = lookup.Id,
            Category = lookup.Category,
            ParentId = lookup.ParentId,
            Code = lookup.Code,
            Label = lookup.Label,
            SortOrder = lookup.SortOrder,
            IsActive = lookup.IsActive,
            Metadata = lookup.Metadata,
            CreatedAt = lookup.CreatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> CreateLookup([FromBody] CreateLookupRequest request)
    {
        // Check unique constraint
        var existing = await _lookupRepository.FindAsync(l =>
            l.Category == request.Category && l.Code == request.Code);
        if (existing.Any())
            return Conflict(new { error = $"Lookup with code '{request.Code}' already exists in category '{request.Category}'" });

        var lookup = new Lookup
        {
            Id = Guid.NewGuid(),
            Category = request.Category,
            ParentId = request.ParentId,
            Code = request.Code,
            Label = request.Label,
            SortOrder = request.SortOrder,
            IsActive = true,
            Metadata = request.Metadata ?? new(),
            CreatedAt = DateTime.UtcNow
        };

        await _lookupRepository.AddAsync(lookup);
        return CreatedAtAction(nameof(GetLookup), new { id = lookup.Id }, new { id = lookup.Id, message = "Lookup created successfully" });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> UpdateLookup(Guid id, [FromBody] UpdateLookupRequest request)
    {
        var lookups = await _lookupRepository.FindAsync(l => l.Id == id);
        var lookup = lookups.FirstOrDefault();
        if (lookup == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Code)) lookup.Code = request.Code;
        if (!string.IsNullOrWhiteSpace(request.Label)) lookup.Label = request.Label;
        if (request.SortOrder.HasValue) lookup.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) lookup.IsActive = request.IsActive.Value;
        if (request.Metadata != null) lookup.Metadata = request.Metadata;

        await _lookupRepository.UpdateAsync(lookup);
        return Ok(new { message = "Lookup updated successfully" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> DeleteLookup(Guid id)
    {
        var lookups = await _lookupRepository.FindAsync(l => l.Id == id);
        var lookup = lookups.FirstOrDefault();
        if (lookup == null) return NotFound();

        // Soft delete — deactivate
        lookup.IsActive = false;
        await _lookupRepository.UpdateAsync(lookup);
        return Ok(new { message = "Lookup deactivated successfully" });
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkLookupRequest request)
    {
        int count = 0;
        foreach (var item in request.Items)
        {
            var existing = await _lookupRepository.FindAsync(l =>
                l.Category == item.Category && l.Code == item.Code);
            if (existing.Any()) continue;

            await _lookupRepository.AddAsync(new Lookup
            {
                Id = Guid.NewGuid(),
                Category = item.Category,
                ParentId = item.ParentId,
                Code = item.Code,
                Label = item.Label,
                SortOrder = item.SortOrder,
                IsActive = true,
                Metadata = item.Metadata ?? new(),
                CreatedAt = DateTime.UtcNow
            });
            count++;
        }
        return Ok(new { count, message = $"{count} lookups created" });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var allLookups = await _lookupRepository.GetAllAsync();
        var categories = allLookups.Select(l => l.Category).Distinct().OrderBy(c => c).ToList();
        return Ok(categories);
    }
}

// DTOs
public class LookupDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = "";
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateLookupRequest
{
    public string Category { get; set; } = "";
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class UpdateLookupRequest
{
    public string? Code { get; set; }
    public string? Label { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class BulkLookupRequest
{
    public List<CreateLookupRequest> Items { get; set; } = new();
}
