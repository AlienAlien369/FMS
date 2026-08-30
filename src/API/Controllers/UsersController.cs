using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly ITenantRepository _tenantRepository;

    public UsersController(
        IGenericRepository<User> userRepository,
        IGenericRepository<Role> roleRepository,
        ITenantRepository tenantRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tenantRepository = tenantRepository;
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User?.Claims?.FirstOrDefault(c => c.Type == "tenant_id");
        return tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] string? status = null)
    {
        var tenantId = GetCurrentTenantId();
        var query = (await _userRepository.FindAsync(u => u.TenantId == tenantId)).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email.Contains(search) || (u.FirstName != null && u.FirstName.Contains(search)) || (u.LastName != null && u.LastName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => status == "active" ? u.IsActive : !u.IsActive);

        var totalCount = query.Count();
        var users = query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                MfaEnabled = u.MfaEnabled,
                RoleId = u.RoleId,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin
            })
            .ToList();

        // Attach role names
        foreach (var user in users)
        {
            if (user.RoleId.HasValue)
            {
                var roles = await _roleRepository.FindAsync(r => r.Id == user.RoleId.Value);
                var role = roles.FirstOrDefault();
                user.RoleName = role?.Name;
                user.Permissions = role?.Permissions ?? new List<string>();
            }
        }

        return Ok(new { items = users, totalCount, pageNumber = page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var users = await _userRepository.FindAsync(u => u.Id == id && u.TenantId == tenantId);
        var user = users.FirstOrDefault();
        if (user == null) return NotFound();

        string? roleName = null;
        List<string> permissions = new();
        if (user.RoleId.HasValue)
        {
            var roles = await _roleRepository.FindAsync(r => r.Id == user.RoleId.Value);
            var role = roles.FirstOrDefault();
            roleName = role?.Name;
            permissions = role?.Permissions ?? new List<string>();
        }

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            MfaEnabled = user.MfaEnabled,
            RoleId = user.RoleId,
            RoleName = roleName,
            Permissions = permissions,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Tenant context required" });

        var existing = await _userRepository.FindAsync(u => u.Email == request.Email && u.TenantId == tenantId);
        if (existing.Any()) return Conflict(new { error = "Email already exists in this tenant" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = request.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { id = user.Id, email = user.Email, message = "User created successfully" });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var users = await _userRepository.FindAsync(u => u.Id == id && u.TenantId == tenantId);
        var user = users.FirstOrDefault();
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName;
        if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName;
        if (request.RoleId.HasValue) user.RoleId = request.RoleId;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.MfaEnabled.HasValue) user.MfaEnabled = request.MfaEnabled.Value;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return Ok(new { message = "User updated successfully" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var users = await _userRepository.FindAsync(u => u.Id == id && u.TenantId == tenantId);
        var user = users.FirstOrDefault();
        if (user == null) return NotFound();

        await _userRepository.DeleteAsync(user);
        return Ok(new { message = "User deleted successfully" });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var tenantId = GetCurrentTenantId();
        var roles = await _roleRepository.FindAsync(r => r.TenantId == tenantId);
        return Ok(roles.Select(r => new { r.Id, r.Name, r.Description, r.Permissions, r.IsSystemRole }));
    }

    [HttpPost("roles")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Permissions = request.Permissions ?? new List<string>(),
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };
        await _roleRepository.AddAsync(role);
        return Ok(new { role.Id, role.Name, role.Description, message = "Role created successfully" });
    }

    [HttpPut("roles/{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var roles = await _roleRepository.FindAsync(r => r.Id == id && r.TenantId == tenantId);
        var role = roles.FirstOrDefault();
        if (role == null) return NotFound(new { title = "Role not found" });
        if (role.IsSystemRole) return BadRequest(new { title = "Cannot modify system roles" });
        role.Name = request.Name ?? role.Name;
        role.Description = request.Description ?? role.Description;
        role.Permissions = request.Permissions ?? role.Permissions;
        await _roleRepository.UpdateAsync(role);
        return Ok(new { message = "Role updated successfully" });
    }

    [HttpDelete("roles/{id:guid}")]
    [Authorize(Roles = "Super Admin,Admin")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        var roles = await _roleRepository.FindAsync(r => r.Id == id && r.TenantId == tenantId);
        var role = roles.FirstOrDefault();
        if (role == null) return NotFound(new { title = "Role not found" });
        if (role.IsSystemRole) return BadRequest(new { title = "Cannot delete system roles" });
        await _roleRepository.DeleteAsync(role);
        return Ok(new { message = "Role deleted successfully" });
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        var tenantId = GetCurrentTenantId();
        var users = await _userRepository.FindAsync(u => u.Id == id && u.TenantId == tenantId);
        var user = users.FirstOrDefault();
        if (user == null) return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return Ok(new { message = "Password reset successfully" });
    }
}

// DTOs
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool MfaEnabled { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? RoleId { get; set; }
}

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public bool? MfaEnabled { get; set; }
    public string? Password { get; set; }
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = "";
}

public class CreateRoleRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
}

public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
}
