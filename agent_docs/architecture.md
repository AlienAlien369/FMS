# FMS System Architecture

## Service Boundaries

### Core Platform Services
| Service | Responsibility | Tech |
|---------|---------------|------|
| Identity Service | Multi-tenant auth, JWT, MFA, SSO-ready | ASP.NET Identity |
| Tenant Registry | CRUD tenants, plans, features, billing | .NET API |
| Config Engine | Dynamic modules, nav, tables, forms, white-label | .NET API |
| Notification Hub | Email (SendGrid), SMS (Twilio), Push (FCM) | .NET API |
| Audit & Compliance | Tamper-proof logs, GDPR export, data retention | .NET API |
| File Service | Tenant-prefixed uploads to R2, image optimization | .NET API |
| Cache Layer | Tenant-scoped Redis sessions and query caching | Redis (Upstash) |

### Telemetry Ingestion
| Service | Responsibility | Tech |
|---------|---------------|------|
| MQTT Broker | Device topic routing: `{tenantId}/{vendorCode}/{deviceId}/FROM` | Mosquitto/EMQX |
| DataCollector | Multi-protocol listener (TCP, MQTT, UDP, HTTP) | .NET Worker |
| Protocol Normalizer | Vendor adapter → Standard Telemetry Model | .NET Worker |
| Event Bus | Tenant-tagged RabbitMQ routing | RabbitMQ |
| MQReceiver | Trip, VLT, Alert, Alarm consumers | .NET Worker |
| AlarmChecker | Per-tenant cron-based alert rule evaluation | .NET Worker |
| SignalR Hub | Real-time device streams to tenant-isolated groups | SignalR |

### Business Modules (Plugin Architecture)
All 8 sectors are Module Federation remotes. Each exposes:
- Routes (lazy-loaded)
- Components (dynamic table, forms, dashboards)
- Services (sector-specific APIs)
- State (sector-specific RxJS stores)

## Data Flow

```
GPS Device (iTriangle/Streamax/Teltonika)
    ↓ TCP/MQTT/UDP/HTTP
MQTT Broker (topic-routed per tenant)
    ↓
DataCollector (parses vendor protocol)
    ↓
Protocol Normalizer (vendor → standard JSON)
    ↓
Event Bus (RabbitMQ, tenant-tagged)
    ↓
Workers (MQReceiver, AlarmChecker)
    ↓
PostgreSQL (structured: vehicles, drivers, trips, alerts)
MongoDB (time-series: telemetry, video events)
R2 (files: video clips, firmware, documents)
    ↓
SignalR (real-time push to Angular clients)
    ↓
Angular Dynamic Components (tables, maps, dashboards)
```

## Multi-Tenancy Strategy

### Tier 1: Shared Database (Small-Medium Tenants)
- Single PostgreSQL instance (Neon)
- Every table has `tenant_id UUID` + `country_code VARCHAR(2)`
- RLS Policy enforces isolation:
  ```sql
  CREATE POLICY tenant_isolation ON vehicles
    USING (tenant_id = current_setting('app.current_tenant')::UUID);
  ```
- Connection string sets `app.current_tenant` per request

### Tier 2: Schema-Per-Tenant (Enterprise)
- Automated provisioning via API
- Isolated schema within shared PostgreSQL instance
- Same connection string, different `search_path`

### Tier 3: Database-Per-Tenant (Government)
- Fully isolated PostgreSQL instance
- Provisioned in tenant's preferred region (GDPR compliance)
- Managed via Neon branching or Railway databases

## Module Federation Architecture

```
Shell App (Host)
├── Core Module (auth, tenant, nav, theming)
├── Shared Module (dynamic table, form renderer, components)
└── Remotes (loaded on demand):
    ├── logistics (localhost:4201)
    ├── taxi (localhost:4202)
    ├── school-bus (localhost:4203)
    ├── ambulance (localhost:4204)
    ├── public-transport (localhost:4205)
    ├── mining (localhost:4206)
    ├── railways (localhost:4207)
    └── law-enforcement (localhost:4208)
```

Remote modules are ONLY downloaded if:
1. Tenant has subscribed to the sector
2. User has permission for the module
3. Route is navigated to (lazy loaded)

## UAT Environment URLs
- **API Gateway:** `https://fms-api-uat.onrender.com`
- **Angular Shell:** `https://fms-web-uat.vercel.app`
- **Platform Admin:** `https://fms-admin-uat.vercel.app`
- **MQTT Broker:** `mqtt://fms-mqtt-uat.onrender.com:1883`
- **PostgreSQL:** Neon UAT branch
- **MongoDB:** Atlas M0 dev cluster
