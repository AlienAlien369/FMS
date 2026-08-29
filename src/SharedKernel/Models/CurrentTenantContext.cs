namespace FMS.SharedKernel.Models;

/// <summary>
/// Request-scoped tenant context resolved by TenantResolutionMiddleware.
/// </summary>
public sealed class CurrentTenantContext
{
    private Guid? _tenantId;

    public Guid TenantId => _tenantId ?? throw new InvalidOperationException("TenantId not resolved. Ensure TenantResolutionMiddleware is registered.");

    public string TenantIdString => TenantId.ToString("D");

    public bool IsResolved => _tenantId.HasValue;

    public void SetTenantId(Guid tenantId) => _tenantId = tenantId;

    public void SetTenantId(string tenantId)
    {
        if (Guid.TryParse(tenantId, out var id))
            _tenantId = id;
    }
}

/// <summary>
/// Represents the current authenticated user with tenant context.
/// </summary>
public sealed class CurrentUserContext
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Plan { get; set; } = "basic";
    public List<string> Permissions { get; set; } = new();
    public string? RoleName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }

    public bool HasPermission(string permission) => Permissions.Contains(permission) || Permissions.Contains("*");
    public bool IsInRole(string role) => RoleName?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>
/// Paginated response wrapper used across all services.
/// </summary>
public sealed class PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// Standard API response envelope.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; } = true;
    public string? Message { get; init; }
    public T? Data { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public List<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new() { Data = data, Message = message };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Errors = new() { error } };
    public static ApiResponse<T> Fail(List<string> errors) => new() { Success = false, Errors = errors };
}

/// <summary>
/// Result type for command handlers.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public string[]? Errors { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result<T> Failure(string[] errors) => new() { IsSuccess = false, Errors = errors };
}

/// <summary>
/// Non-generic result type (for void commands).
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}
