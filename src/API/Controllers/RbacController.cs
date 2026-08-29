using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/rbac")]
[Authorize]
public class RbacController : ControllerBase
{
    private readonly IGenericRepository<FormRoleMapping> _roleMappingRepository;
    private readonly IGenericRepository<FormCompanyMapping> _companyMappingRepository;
    private readonly IGenericRepository<FormColumnConfig> _columnConfigRepository;
    private readonly IGenericRepository<FormMaster> _formRepository;
    private readonly IGenericRepository<Role> _roleRepository;

    public RbacController(
        IGenericRepository<FormRoleMapping> roleMappingRepository,
        IGenericRepository<FormCompanyMapping> companyMappingRepository,
        IGenericRepository<FormColumnConfig> columnConfigRepository,
        IGenericRepository<FormMaster> formRepository,
        IGenericRepository<Role> roleRepository)
    {
        _roleMappingRepository = roleMappingRepository;
        _companyMappingRepository = companyMappingRepository;
        _columnConfigRepository = columnConfigRepository;
        _formRepository = formRepository;
        _roleRepository = roleRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    // ── Form Role Mapping ──
    [HttpGet("role-forms")]
    public async Task<IActionResult> GetRoleFormMappings([FromQuery] Guid? roleId = null, [FromQuery] string? formCategory = null)
    {
        var tenantId = GetCurrentTenantId();
        var mappings = await _roleMappingRepository.FindAsync(m => m.TenantId == tenantId);
        var query = mappings.AsQueryable();

        if (roleId.HasValue) query = query.Where(m => m.RoleId == roleId.Value);

        // Get forms and optionally filter by category
        var forms = await _formRepository.GetAllAsync();
        var formDict = forms.ToDictionary(f => f.Id);

        var result = query.Select(m => new RoleFormMappingDto
        {
            Id = m.Id, RoleId = m.RoleId, FormId = m.FormId,
            FormName = formDict.ContainsKey(m.FormId) ? formDict[m.FormId].FormName : "Unknown",
            ControllerName = formDict.ContainsKey(m.FormId) ? formDict[m.FormId].ControllerName : "",
            CanView = m.CanView, CanAdd = m.CanAdd, CanEdit = m.CanEdit, CanDelete = m.CanDelete
        }).ToList();

        return Ok(result);
    }

    [HttpPost("role-forms")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> SetRoleFormMapping([FromBody] SetRoleFormMappingRequest request)
    {
        var tenantId = GetCurrentTenantId();

        var existing = await _roleMappingRepository.FindAsync(m =>
            m.TenantId == tenantId && m.RoleId == request.RoleId && m.FormId == request.FormId);
        var mapping = existing.FirstOrDefault();

        if (mapping == null)
        {
            mapping = new FormRoleMapping
            {
                Id = Guid.NewGuid(), TenantId = tenantId, RoleId = request.RoleId, FormId = request.FormId,
                CanView = request.CanView, CanAdd = request.CanAdd, CanEdit = request.CanEdit, CanDelete = request.CanDelete,
                CreatedAt = DateTime.UtcNow
            };
            await _roleMappingRepository.AddAsync(mapping);
        }
        else
        {
            mapping.CanView = request.CanView;
            mapping.CanAdd = request.CanAdd;
            mapping.CanEdit = request.CanEdit;
            mapping.CanDelete = request.CanDelete;
            await _roleMappingRepository.UpdateAsync(mapping);
        }

        return Ok(new { message = "Permission updated" });
    }

    [HttpPost("role-forms/bulk")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> BulkSetRoleFormMappings([FromBody] BulkRoleFormMappingRequest request)
    {
        var tenantId = GetCurrentTenantId();
        int count = 0;

        foreach (var item in request.Mappings)
        {
            var existing = await _roleMappingRepository.FindAsync(m =>
                m.TenantId == tenantId && m.RoleId == request.RoleId && m.FormId == item.FormId);
            var mapping = existing.FirstOrDefault();

            if (mapping == null)
            {
                await _roleMappingRepository.AddAsync(new FormRoleMapping
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, RoleId = request.RoleId, FormId = item.FormId,
                    CanView = item.CanView, CanAdd = item.CanAdd, CanEdit = item.CanEdit, CanDelete = item.CanDelete,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                mapping.CanView = item.CanView;
                mapping.CanAdd = item.CanAdd;
                mapping.CanEdit = item.CanEdit;
                mapping.CanDelete = item.CanDelete;
                await _roleMappingRepository.UpdateAsync(mapping);
            }
            count++;
        }

        return Ok(new { count, message = $"{count} permissions updated" });
    }

    // ── Form Company Mapping ──
    [HttpGet("company-forms")]
    public async Task<IActionResult> GetCompanyFormMappings()
    {
        var tenantId = GetCurrentTenantId();
        var mappings = await _companyMappingRepository.FindAsync(m => m.TenantId == tenantId);
        var forms = await _formRepository.GetAllAsync();
        var formDict = forms.ToDictionary(f => f.Id);

        var result = mappings.Select(m => new CompanyFormMappingDto
        {
            Id = m.Id, FormId = m.FormId,
            FormName = formDict.ContainsKey(m.FormId) ? formDict[m.FormId].FormName : "Unknown",
            IsEnabled = m.IsEnabled
        }).ToList();

        return Ok(result);
    }

    [HttpPost("company-forms")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> SetCompanyFormMapping([FromBody] SetCompanyFormMappingRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var existing = await _companyMappingRepository.FindAsync(m =>
            m.TenantId == tenantId && m.FormId == request.FormId);
        var mapping = existing.FirstOrDefault();

        if (mapping == null)
        {
            await _companyMappingRepository.AddAsync(new FormCompanyMapping
            {
                Id = Guid.NewGuid(), TenantId = tenantId, FormId = request.FormId,
                IsEnabled = request.IsEnabled, CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            mapping.IsEnabled = request.IsEnabled;
            await _companyMappingRepository.UpdateAsync(mapping);
        }

        return Ok(new { message = "Company form mapping updated" });
    }

    // ── Form Column Config ──
    [HttpGet("column-configs")]
    public async Task<IActionResult> GetColumnConfigs([FromQuery] Guid? formId = null)
    {
        var tenantId = GetCurrentTenantId();
        var configs = await _columnConfigRepository.FindAsync(c => c.TenantId == tenantId);
        if (formId.HasValue) configs = configs.Where(c => c.FormId == formId.Value).ToList();

        var result = configs.OrderBy(c => c.SortOrder).Select(c => new ColumnConfigDto
        {
            Id = c.Id, FormId = c.FormId, ColumnName = c.ColumnName,
            DisplayName = c.DisplayName, IsActive = c.IsActive, SortOrder = c.SortOrder
        }).ToList();

        return Ok(result);
    }

    [HttpPost("column-configs")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> SetColumnConfig([FromBody] SetColumnConfigRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var existing = await _columnConfigRepository.FindAsync(c =>
            c.TenantId == tenantId && c.FormId == request.FormId && c.ColumnName == request.ColumnName);
        var config = existing.FirstOrDefault();

        if (config == null)
        {
            await _columnConfigRepository.AddAsync(new FormColumnConfig
            {
                Id = Guid.NewGuid(), TenantId = tenantId, FormId = request.FormId,
                ColumnName = request.ColumnName, DisplayName = request.DisplayName,
                IsActive = request.IsActive, SortOrder = request.SortOrder, CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            config.DisplayName = request.DisplayName;
            config.IsActive = request.IsActive;
            config.SortOrder = request.SortOrder;
            await _columnConfigRepository.UpdateAsync(config);
        }

        return Ok(new { message = "Column config updated" });
    }
}

// DTOs
public class RoleFormMappingDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid FormId { get; set; }
    public string FormName { get; set; } = "";
    public string ControllerName { get; set; } = "";
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class SetRoleFormMappingRequest
{
    public Guid RoleId { get; set; }
    public Guid FormId { get; set; }
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class BulkRoleFormMappingRequest
{
    public Guid RoleId { get; set; }
    public List<SetRoleFormMappingRequest> Mappings { get; set; } = new();
}

public class CompanyFormMappingDto
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public string FormName { get; set; } = "";
    public bool IsEnabled { get; set; }
}

public class SetCompanyFormMappingRequest
{
    public Guid FormId { get; set; }
    public bool IsEnabled { get; set; }
}

public class ColumnConfigDto
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public string ColumnName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class SetColumnConfigRequest
{
    public Guid FormId { get; set; }
    public string ColumnName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
