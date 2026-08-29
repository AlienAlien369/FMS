# FMS Security & Compliance

## Authentication Flow
1. User logs in with email/password (or SSO)
2. Identity Service validates credentials + tenant context
3. Returns JWT access token (15 min expiry) + refresh token (7 days)
4. Angular stores tokens in httpOnly cookie (preferred) or secure localStorage
5. Every API request includes `Authorization: Bearer {token}`
6. Tenant middleware extracts `tenant_id` from JWT claim
7. RLS policy enforces `tenant_id` match on every query

## GDPR Compliance
- **Data Residency:** Tenant data stored in region specified by `data_residency_region`
- **Right to Erasure:** `DELETE /api/v1/admin/tenants/{id}/gdpr-export` initiates export
- **Audit Trail:** Every CRUD operation logged to `audit_logs` table
- **Encryption:** Data encrypted at rest (Neon default) and in transit (TLS 1.3)
- **Retention:** Automated purge of telemetry after retention period (configurable per tenant)

## Input Validation
- All API inputs validated via FluentValidation
- SQL injection prevention: EF Core parameterized queries only
- XSS prevention: Angular auto-escapes by default, sanitize any innerHTML
- File upload: Virus scan (ClamAV), size limits, type whitelist

## CORS
Configured per tenant domain in `tenants.settings->allowedOrigins`

## Security Headers
- Content-Security-Policy (CSP)
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- Strict-Transport-Security (HSTS)
- Referrer-Policy: strict-origin-when-cross-origin
