using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/forms")]
[Authorize]
public class FormMastersController : ControllerBase
{
    private readonly IGenericRepository<FormMaster> _formRepository;

    public FormMastersController(IGenericRepository<FormMaster> formRepository)
    {
        _formRepository = formRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetForms([FromQuery] string? search = null, [FromQuery] bool activeOnly = true)
    {
        var forms = await _formRepository.GetAllAsync();
        var query = forms.AsQueryable();

        if (activeOnly) query = query.Where(f => f.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.FormName.Contains(search) || f.ControllerName.Contains(search));

        var result = query.OrderBy(f => f.FormName).Select(f => new FormMasterDto
        {
            Id = f.Id, FormName = f.FormName, ControllerName = f.ControllerName,
            ActionName = f.ActionName, ClassName = f.ClassName, ParentFormId = f.ParentFormId,
            AreaName = f.AreaName, Platform = f.Platform, IsActive = f.IsActive, CreatedAt = f.CreatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetForm(Guid id)
    {
        var forms = await _formRepository.FindAsync(f => f.Id == id);
        var form = forms.FirstOrDefault();
        if (form == null) return NotFound();
        return Ok(new FormMasterDto
        {
            Id = form.Id, FormName = form.FormName, ControllerName = form.ControllerName,
            ActionName = form.ActionName, ClassName = form.ClassName, ParentFormId = form.ParentFormId,
            AreaName = form.AreaName, Platform = form.Platform, IsActive = form.IsActive, CreatedAt = form.CreatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> CreateForm([FromBody] CreateFormRequest request)
    {
        var existing = await _formRepository.FindAsync(f => f.FormName == request.FormName);
        if (existing.Any()) return Conflict(new { error = $"Form '{request.FormName}' already exists" });

        var form = new FormMaster
        {
            Id = Guid.NewGuid(), FormName = request.FormName, ControllerName = request.ControllerName,
            ActionName = request.ActionName, ClassName = request.ClassName, ParentFormId = request.ParentFormId,
            AreaName = request.AreaName, Platform = request.Platform ?? "Web", IsActive = true, CreatedAt = DateTime.UtcNow
        };
        await _formRepository.AddAsync(form);
        return CreatedAtAction(nameof(GetForm), new { id = form.Id }, new { id = form.Id, message = "Form created" });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> UpdateForm(Guid id, [FromBody] UpdateFormRequest request)
    {
        var forms = await _formRepository.FindAsync(f => f.Id == id);
        var form = forms.FirstOrDefault();
        if (form == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.FormName)) form.FormName = request.FormName;
        if (!string.IsNullOrWhiteSpace(request.ControllerName)) form.ControllerName = request.ControllerName;
        if (!string.IsNullOrWhiteSpace(request.ActionName)) form.ActionName = request.ActionName;
        if (request.ClassName != null) form.ClassName = request.ClassName;
        if (request.ParentFormId.HasValue) form.ParentFormId = request.ParentFormId;
        if (request.AreaName != null) form.AreaName = request.AreaName;
        if (request.Platform != null) form.Platform = request.Platform;
        if (request.IsActive.HasValue) form.IsActive = request.IsActive.Value;

        await _formRepository.UpdateAsync(form);
        return Ok(new { message = "Form updated" });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> DeleteForm(Guid id)
    {
        var forms = await _formRepository.FindAsync(f => f.Id == id);
        var form = forms.FirstOrDefault();
        if (form == null) return NotFound();

        form.IsActive = false;
        await _formRepository.UpdateAsync(form);
        return Ok(new { message = "Form deactivated" });
    }
}

public class FormMasterDto
{
    public Guid Id { get; set; }
    public string FormName { get; set; } = "";
    public string ControllerName { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string? ClassName { get; set; }
    public Guid? ParentFormId { get; set; }
    public string? AreaName { get; set; }
    public string Platform { get; set; } = "Web";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFormRequest
{
    public string FormName { get; set; } = "";
    public string ControllerName { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string? ClassName { get; set; }
    public Guid? ParentFormId { get; set; }
    public string? AreaName { get; set; }
    public string? Platform { get; set; }
}

public class UpdateFormRequest
{
    public string? FormName { get; set; }
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public string? ClassName { get; set; }
    public Guid? ParentFormId { get; set; }
    public string? AreaName { get; set; }
    public string? Platform { get; set; }
    public bool? IsActive { get; set; }
}
