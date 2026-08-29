# RGBSI Fleet Management SaaS (FMS)

## Stack
- **Frontend:** Angular 17+, Module Federation, RxJS, Tailwind CSS, Leaflet.js
- **Backend:** .NET 8 Web API, Clean Architecture (Domain → Application → Infrastructure → API)
- **Pattern:** CQRS with MediatR, Repository + Unit of Work, EF Core PostgreSQL
- **Databases:** PostgreSQL (Neon, RLS-enabled) + MongoDB Atlas (telemetry time-series)
- **Cache:** Redis (Upstash, tenant-scoped keys)
- **Message Bus:** RabbitMQ (CloudAMQP, tenant-tagged routing keys)
- **MQTT:** Mosquitto self-hosted (Railway) for device ingestion
- **Real-Time:** SignalR (tenant-isolated hub groups: `{tenantId}:{deviceId}`)
- **Storage:** Cloudflare R2 (tenant-prefixed paths, encrypted at rest)
- **Auth:** ASP.NET Identity, JWT (short expiry + refresh tokens), MFA-ready
- **Maps:** Leaflet + OpenStreetMap (free)
- **Hosting:** Railway (containers), Cloudflare Pages (Angular static), Cloudflare CDN
- **CI/CD:** GitHub Actions (2000 min/month free)
- **Monitoring:** Grafana Cloud (free tier)

## Architecture Principles
1. **Multi-Tenancy First:** Every table has `tenant_id`. Every API call resolves tenant from subdomain/JWT. PostgreSQL RLS enforces isolation.
2. **Dynamic Everything:** Modules, navbar, table columns, forms, alerts, reports — all API-driven and user-configurable.
3. **Zero-Code Extensibility:** New GPS vendors onboarded via JSON schema upload. New sectors via plugin architecture.
4. **Free-Tier Optimized:** Every service choice prioritizes free tier. Scale to paid only when metrics demand it.
5. **Country Compliance:** GDPR data residency, multi-currency, multi-language (i18n), RTL support (Arabic/Hebrew).

## Conventions

### Backend (.NET)
- **Clean Architecture folders:** `Domain/`, `Application/`, `Infrastructure/`, `API/`
- **CQRS:** Commands in `Application/Features/{Module}/Commands/`, Queries in `Queries/`
- **MediatR:** Every use case is a command or query with a handler
- **DTOs:** Use records for request/response DTOs. Prefix with module name.
- **Entity Framework:** Use `HasQueryFilter` for tenant isolation. Never disable RLS.
- **Migrations:** EF Core migrations in `Infrastructure/Migrations/`. One migration per feature branch.
- **API Routes:** `api/v1/{module}/{resource}` — tenant resolved via middleware
- **Background Workers:** Use `IHostedService` for MQTT consumers, `AlarmChecker` cron jobs
- **Device Adapter:** Vendor configs stored in `device_vendors.schema_config` (JSONB). Adapters loaded dynamically.
- **SignalR:** Hub groups named `{tenantId}:{deviceId}`. Never broadcast to all.
- **Redis Keys:** `{tenantId}:{cacheType}:{entityId}` — always scoped by tenant

### Frontend (Angular)
- **Module Federation:** Shell app loads sector modules dynamically. Disabled modules = 0 bytes.
- **Lazy Loading:** Every sector module is a remote MF entry. Core platform is host.
- **Dynamic Components:**
  - Tables: `DynamicTableComponent` reads column config from `user_preferences`
  - Forms: `JsonSchemaFormComponent` renders reactive forms from JSON schema
  - Nav: `DynamicNavService` fetches menu from `/api/v1/config/navigation`
- **State Management:** RxJS BehaviorSubjects for tenant/user context. No NgRx (keep it light).
- **i18n:** `$localize` with runtime locale loading. RTL via `dir="rtl"` on `<html>`.
- **White-Label:** CSS variables injected at runtime from `/api/v1/config/branding`
- **SignalR Client:** Auto-connects on login. Reconnect with exponential backoff.
- **File Structure:**
  ```
  src/app/
  ├── core/           (auth, tenant, http interceptors, guards)
  ├── shared/         (dynamic table, form renderer, components)
  ├── shell/          (layout, navbar, white-label themer)
  └── remotes/        (module federation remotes — one per sector)
  ```

### Database
- **PostgreSQL:** All structured data. RLS policy on EVERY table:
  ```sql
  CREATE POLICY tenant_isolation ON {table}
    USING (tenant_id = current_setting('app.current_tenant')::UUID);
  ```
- **MongoDB:** Telemetry, raw device payloads, video events. Shard key: `{tenantId: 1, countryCode: 1}`
- **Naming:** Snake_case for SQL, camelCase for MongoDB documents.
- **Indexes:** Always index `tenant_id`, `created_at`, and query fields. Partial indexes for soft-deleted rows.

### DevOps
- **Docker:** Every service has a `Dockerfile`. `docker-compose.yml` for local dev.
- **Environment:** `appsettings.Development.json` (local), `appsettings.Staging.json` (Railway), `appsettings.Production.json`
- **Secrets:** GitHub Secrets → GitHub Actions → Railway env vars. NEVER commit secrets.
- **Branching:** `main` (production), `staging`, `feature/{module}-{description}`. PR required for `main`.

## Commands
```bash
# Local Development
docker-compose up -d                    # Start PostgreSQL, MongoDB, Redis, RabbitMQ, Mosquitto
dotnet run --project src/API            # Start .NET API (localhost:5000)
ng serve --project shell                # Start Angular shell (localhost:4200)
ng serve --project logistics            # Start logistics remote (localhost:4201)

# Database
dotnet ef migrations add {Name} --project src/Infrastructure --startup-project src/API
dotnet ef database update --project src/Infrastructure --startup-project src/API

# Testing
dotnet test                             # Run all backend tests
ng test --watch=false                   # Run frontend unit tests
ng e2e                                  # Run Playwright E2E tests

# Build & Deploy
dotnet publish -c Release -o ./publish  # Build API container
ng build --configuration=production     # Build Angular shell + remotes

# Infrastructure
terraform plan -var-file=staging.tfvars   # Preview infra changes
terraform apply -var-file=staging.tfvars  # Apply infra changes
```

## Do Not
- **NEVER** disable PostgreSQL RLS for "convenience" or debugging.
- **NEVER** hardcode tenant IDs, connection strings, or API keys in source code.
- **NEVER** add a new dependency (NuGet/npm) without documenting why in the PR description.
- **NEVER** write raw SQL without parameterization — always use EF Core or Dapper with parameters.
- **NEVER** store tenant data in unscoped Redis keys.
- **NEVER** commit `.env` files, `appsettings.Production.json`, or certificate files.
- **NEVER** allow SignalR messages to leak across tenant boundaries.
- **NEVER** run `rm -rf`, `git push origin main`, or `docker system prune` without explicit confirmation.

## Deep Reference
- `agent_docs/architecture.md` — System design, service boundaries, data flow
- `agent_docs/database.md` — Full PostgreSQL + MongoDB schema, RLS policies, indexes
- `agent_docs/api.md` — OpenAPI spec patterns, DTO standards, route conventions
- `agent_docs/frontend.md` — Angular Module Federation, dynamic components, theming
- `agent_docs/device_adapter.md` — Vendor JSON schema, protocol normalizer, MQTT topics
- `agent_docs/security.md` — Auth flow, GDPR compliance, audit logging, encryption
- `agent_docs/deployment.md` — Railway, Neon, Cloudflare, Terraform, CI/CD pipelines
- `agent_docs/8_sectors.md` — Feature matrix per sector, module boundaries
- `agent_docs/onboarding.md` — Tenant self-service provisioning flow
- `agent_docs/testing.md` — Unit test patterns, integration tests, E2E with Playwright
