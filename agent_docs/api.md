# FMS API Standards

## Route Convention
```
/api/v1/{module}/{resource}
```

Tenant is resolved via middleware (subdomain → header → JWT claim), NOT in URL path.

Examples:
- `GET /api/v1/fleet/vehicles`
- `POST /api/v1/logistics/trips`
- `GET /api/v1/config/navigation`
- `POST /api/v1/auth/login`

## Tenant Resolution
1. **Subdomain:** `acme-logistics.fms-uat.vercel.app` → extract `acme-logistics`
2. **Header:** `X-Tenant-ID: acme-logistics`
3. **JWT Claim:** `tenant_id` inside access token

Resolution order: Subdomain → Header → JWT → 401 Unauthorized

## DTO Standards
Use C# records. Prefix with module name.

```csharp
// Request
public record CreateVehicleRequest(
    string VehicleNumber,
    string Type,
    string? Model,
    int? Year,
    string? FuelType,
    string? GpsDeviceId
);

// Response
public record VehicleResponse(
    Guid Id,
    string VehicleNumber,
    string Type,
    string? Model,
    int? Year,
    string Status,
    DateTime CreatedAt
);

// List Response
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
```

## Error Response
```json
{
  "error": {
    "code": "VEHICLE_NOT_FOUND",
    "message": "Vehicle with ID 'abc-123' not found",
    "details": {
      "vehicleId": "abc-123"
    },
    "timestamp": "2026-08-28T04:15:00Z",
    "traceId": "00-abc123-def456-01"
  }
}
```

## Pagination
```
GET /api/v1/fleet/vehicles?page=1&pageSize=25&sort=vehicleNumber&order=asc
```

## Feature Gate Middleware
If a module is disabled for the tenant, return:
```json
{ "error": { "code": "MODULE_DISABLED", "message": "Module 'logistics' is not enabled for this tenant" } }
```

## Rate Limiting
Per-tenant quotas enforced at API Gateway:
- Basic: 100 req/min
- Pro: 1000 req/min
- Enterprise: 10000 req/min

Headers returned:
- `X-RateLimit-Limit`
- `X-RateLimit-Remaining`
- `X-RateLimit-Reset`

## UAT Endpoints
- **Base URL:** `https://fms-api-uat.onrender.com/api/v1`
- **Swagger:** `https://fms-api-uat.onrender.com/swagger`
- **Health:** `https://fms-api-uat.onrender.com/health`
