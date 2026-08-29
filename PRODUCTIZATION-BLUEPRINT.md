# RGBSI Fleet Management System — Productization Blueprint

> **Date:** August 29, 2026  
> **Version:** 1.0  
> **Status:** Implementation In Progress

---

## A. EXISTING FEATURE INVENTORY

### A1. Entities (Domain Layer)

| Entity | Fields | Tenant Scoped | JSON Props | Relationships |
|--------|--------|---------------|------------|---------------|
| **Tenant** | Id, Name, Subdomain, CustomDomain, CountryCode, Timezone, Currency, Plan, Status, DataResidencyRegion | ❌ (IS the tenant) | Settings | 1→N Users, Roles, Features, Vehicles, Drivers, Devices |
| **User** | Id, Email, PasswordHash, FirstName, LastName, RoleId, MfaEnabled, IsActive, LastLogin | ✅ TenantId | Preferences | N→1 Tenant, Role |
| **Role** | Id, Name, Description, IsSystemRole | ✅ TenantId | Permissions (List\<string\>) | N→1 Tenant |
| **Feature** | Id, Module, FeatureName, Enabled | ✅ TenantId | Config | N→1 Tenant |
| **Vehicle** | Id, VehicleNumber, Type, Model, Year, FuelType, GpsDeviceId, Status | ✅ TenantId | Metadata | N→1 Tenant, 1→N Devices |
| **Driver** | Id, FirstName, LastName, LicenseNumber, LicenseExpiry, Phone, BehaviorScore, Status | ✅ TenantId | Documents | N→1 Tenant, User |
| **Device** | Id, Imei, SerialNumber, Model, FirmwareVersion, Status, LastSeen, LastSpeed, SignalStrength, BatteryLevel, InstalledAt | ✅ TenantId | Config | N→1 Tenant, Vendor, Vehicle, Driver |
| **DeviceVendor** | Id, Name, Code, Protocol, DefaultPort, SupportsVideo/Fuel/Temperature/CanBus, AdapterVersion, IsActive | ❌ (global) | SchemaConfig | 1→N Devices |
| **DeviceCommand** | Id, CommandType, Status, SentAt, AcknowledgedAt, ErrorMessage, CreatedBy | ✅ TenantId | Payload, ResponsePayload | N→1 Device, User |
| **UserPreference** | Id, Page, PreferenceType | ❌ (UserId FK) | Config | N→1 User |
| **AuditLog** | Id, Action, EntityType, EntityId, OldValue, NewValue, IpAddress, UserAgent | ✅ TenantId | ❌ | — |

### A2. API Controllers

| Controller | Route | Methods | Auth | CQRS |
|-----------|-------|---------|------|------|
| **AuthController** | `/api/v1/auth` | POST login | ❌ | MediatR LoginCommand |
| **TenantsController** | `/api/v1/tenants` | POST onboard, GET check-subdomain | ❌ | MediatR |
| **UsersController** | `/api/v1/users` | GET list, GET byId, POST create, PUT update, DELETE, GET roles, POST reset-password | ✅ | Direct repo |
| **SettingsController** | `/api/v1/settings` | GET/PUT tenant, GET/PUT features, GET/PUT preferences, GET stats | ✅ | Direct repo |
| **VehiclesController** | `/api/v1/fleet/vehicles` | GET list, POST create | ✅ | MediatR |
| **DriversController** | `/api/v1/fleet/drivers` | GET list, POST create | ✅ | MediatR |
| **DevicesController** | `/api/v1/fleet/devices` | GET list, POST create, POST command | ✅ | MediatR |
| **ConfigController** | `/api/v1/config` | GET navigation, GET branding | ✅ | MediatR |

### A3. Frontend Pages (preview.html SPA)

| Page | CRUD | Live API | Map | Reference |
|------|------|----------|-----|-----------|
| Login | Auth | ✅ `/auth/login` | ❌ | — |
| Operations Overview | Read | ✅ `/fleet/vehicles` | ✅ Leaflet | — |
| Vehicle Directory | Table | ✅ `/fleet/vehicles` | ❌ | — |
| Driver Hub | Table | ✅ `/fleet/drivers` | ❌ | — |
| Device Fleet | Table | ✅ `/fleet/devices` | ❌ | — |
| User Management | CRUD | ✅ `/users` | ❌ | Binary Semantics User Master |
| Configuration | CRUD | ✅ `/settings/*` | ❌ | Binary Semantics Company Master |
| Role & Permissions | Matrix | ✅ `/users/roles` | ❌ | Binary Semantics Form Role Mapping |
| Feature Management | Toggle | ✅ `/settings/features` | ❌ | — |
| Subscription | Form | ❌ (mock) | ❌ | Binary Semantics Subscription Master |
| Route Management | Form+Map | ❌ (mock) | ✅ Leaflet | Binary Semantics Route Management |
| Geofence Management | Form+Map | ❌ (mock) | ✅ Leaflet | Binary Semantics Geofence |
| Incident Center | Table | ❌ (mock) | ❌ | — |
| Active Deliveries | Table | ❌ (mock) | ❌ | — |
| Fuel Analytics | Dashboard | ❌ (mock) | ❌ | — |
| Maintenance Studio | Placeholder | ❌ | ❌ | — |
| Insight Builder | Dashboard | ❌ (mock) | ❌ | — |

### A4. Existing Hardcoded Configuration

| Item | Current | Should Be Dynamic |
|------|---------|-------------------|
| CORS origins | Hardcoded in Program.cs | ✅ Tenant settings |
| Navigation menu | Hardcoded in frontend NAV object | ✅ DB-driven |
| Roles/Permissions | Seeded, stored as JSON list | ✅ Full RBAC tables |
| Feature flags | Seeded, per-tenant toggle | Already dynamic |
| Branding (colors, logo) | Hardcoded in ConfigController | ✅ Tenant settings |
| Country/State/City | Not implemented | ✅ Lookup tables |
| Vehicle types | Not implemented | ✅ Lookup table |
| Device vendors | Hardcoded seed | ✅ Already dynamic |

---

## B. DYNAMICIZATION MATRIX

| Existing Item | Current Implementation | Should Be Dynamic? | Config Type | Backend Change | Frontend Change | Priority |
|--------------|----------------------|-------------------|-------------|----------------|-----------------|----------|
| Country list | Not implemented | ✅ Yes | Lookup table | New `Lookup` entity + controller | Dropdowns read from API | P0 |
| State list | Not implemented | ✅ Yes | Lookup table (parent=Country) | Same Lookup entity | Cascading dropdowns | P0 |
| City list | Not implemented | ✅ Yes | Lookup table (parent=State) | Same Lookup entity | Cascading dropdowns | P0 |
| Vehicle types | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdowns | P0 |
| Fuel types | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdowns | P0 |
| Device protocols | Hardcoded in DeviceVendor | ✅ Yes | Lookup table | Same Lookup entity | Dropdowns | P0 |
| Role permissions | `List<string>` JSON in Role | ✅ Yes | Junction table | New `FormRoleMapping` entity | Permission matrix | P0 |
| Company forms | Not implemented | ✅ Yes | CRUD entity | New `FormMaster` entity | Full CRUD page | P0 |
| Client master | Not implemented | ✅ Yes | CRUD entity | New `Client` entity | Full CRUD page | P0 |
| Routes | Not implemented | ✅ Yes | CRUD entity | New `Route` entity | Form + map + table | P0 |
| Geofences | Not implemented | ✅ Yes | CRUD entity | New `Geofence` entity | Form + map + table | P0 |
| Subscriptions | Not implemented | ✅ Yes | CRUD entity | New `Subscription` entity | Form + table | P0 |
| Notifications | Not implemented | ✅ Yes | CRUD entity | New `Notification` entity | List + actions | P1 |
| Audit logs | Entity exists, never written | ✅ Yes | Auto-capture | Middleware interceptor | Read-only table | P1 |
| Navigation menu | Hardcoded in frontend JS | ✅ Yes | DB-driven | New `NavigationItem` entity | Dynamic sidebar | P1 |
| Form columns | Not implemented | ✅ Yes | Per-form config | New `FormColumnConfig` entity | Toggle matrix | P1 |
| Company→Form mapping | Not implemented | ✅ Yes | Junction table | New `FormCompanyMapping` entity | Permission matrix | P1 |
| Subscription packages | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdown | P1 |
| Payment modes | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdown | P1 |
| Location types | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdown | P0 |
| Route types | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdown | P0 |
| Incident severity | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdown | P1 |
| Incident status | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdown | P1 |
| Delivery status | Not implemented | ✅ Yes | Lookup table | Same Lookup entity | Dropdown | P1 |
| JWT Secret | Env var | ❌ No | Env var | — | — | — |
| Database URL | Env var | ❌ No | Env var | — | — | — |

---

## C. NEW PRODUCT MODULES

### C1. Lookups (P0 — Foundation)
Dynamic dropdown values. Single `Lookup` entity with `Category` + `ParentId` for cascading.

**Categories:** Country, State, City, VehicleType, FuelType, DeviceProtocol, RouteType, LocationType, GeofenceColor, SubscriptionPackage, PaymentMode, IncidentSeverity, IncidentStatus, DeliveryStatus, CompanyType, ConsigneeCategory, UserFor

### C2. Client Master (P0 — Core CRUD)
Company's clients/consignees with full address, billing, contact, GST/PAN/CIN.

### C3. Form Master (P0 — Configuration)
System forms/pages registry — maps Form Name → Controller → Action for RBAC.

### C4. Route Management (P0 — Fleet Operations)
Routes with start/end locations, waypoints, distance, route type, map visualization.

### C5. Geofence Management (P0 — Fleet Operations)
Geofences with name, location, type, radius, color, map overlay.

### C6. Subscription Management (P0 — Billing)
Per-tenant subscription tracking with packages, invoices, payment modes.

### C7. RBAC / Permissions (P1 — Security)
- **FormRoleMapping**: Which roles can access which forms with what rights (View/Add/Edit/Delete)
- **FormCompanyMapping**: Which forms are enabled per company

### C8. Navigation Configuration (P1 — UI)
Dynamic sidebar driven from DB, per-plan module visibility.

### C9. Form Column Configuration (P1 — UI Customization)
Per-form column visibility settings per company/transporter.

### C10. Notifications (P1 — Communication)
In-app notification center with types, read/unread status.

### C11. Audit Trail (P1 — Compliance)
Auto-captured change log with old/new values, user, IP, timestamp.

### C12. Dashboard / Reports (P2 — Analytics)
Configurable dashboard widgets, report templates.

---

## D. DATABASE PLAN

### D1. Existing Tables (via EF Core)

```
tenants          (11 columns)
users            (12 columns)  
roles            (7 columns)
features         (8 columns)
vehicles         (12 columns)
drivers          (13 columns)
devices          (18 columns)
device_vendors   (13 columns)
device_commands  (12 columns)
user_preferences (7 columns)
audit_logs       (12 columns)
```

### D2. New Tables

```sql
-- P0: Lookups (all dropdowns)
lookups (
    id UUID PRIMARY KEY,
    category VARCHAR(50) NOT NULL,        -- 'Country', 'State', 'VehicleType', etc.
    parent_id UUID REFERENCES lookups(id), -- For cascading (State→Country)
    code VARCHAR(20) NOT NULL,            -- 'IN', 'US', 'SA'
    label VARCHAR(100) NOT NULL,          -- 'India', 'United States'
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    metadata JSONB DEFAULT '{}',          -- Extra data (phone code, currency, etc.)
    created_at TIMESTAMPTZ DEFAULT NOW()
)
CREATE INDEX idx_lookups_category ON lookups(category);
CREATE INDEX idx_lookups_parent ON lookups(parent_id);

-- P0: Client Master
clients (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    parent_client_id UUID REFERENCES clients(id),
    company_name VARCHAR(200),
    client_name VARCHAR(200) NOT NULL,
    client_code VARCHAR(50) NOT NULL,
    address TEXT,
    pin_code VARCHAR(20),
    country_id UUID REFERENCES lookups(id),
    state_id UUID REFERENCES lookups(id),
    city_id UUID REFERENCES lookups(id),
    latitude DECIMAL(10,7),
    longitude DECIMAL(10,7),
    billing_address_same BOOLEAN DEFAULT false,
    billing_address TEXT,
    billing_pin_code VARCHAR(20),
    billing_country_id UUID REFERENCES lookups(id),
    billing_state_id UUID REFERENCES lookups(id),
    billing_city_id UUID REFERENCES lookups(id),
    company_phone VARCHAR(20),
    contact_person VARCHAR(100),
    contact_no VARCHAR(20),
    alt_contact_no VARCHAR(20),
    contact_email VARCHAR(100),
    mobile_no VARCHAR(20),
    email_id VARCHAR(100),
    alt_email_id VARCHAR(100),
    pan_no VARCHAR(20),
    gst_no VARCHAR(30),
    cin_no VARCHAR(30),
    consignee_category_id UUID REFERENCES lookups(id),
    is_contract_signed BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
)

-- P0: Form Master (system pages for RBAC)
form_masters (
    id UUID PRIMARY KEY,
    form_name VARCHAR(100) NOT NULL,
    controller_name VARCHAR(100) NOT NULL,
    action_name VARCHAR(100) NOT NULL,
    class_name VARCHAR(100),
    parent_form_id UUID REFERENCES form_masters(id),
    area_name VARCHAR(50),
    platform VARCHAR(20) DEFAULT 'Web',  -- Web, Mobile, Both
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW()
)

-- P0: Routes
routes (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    route_name VARCHAR(100) NOT NULL,
    start_location VARCHAR(200) NOT NULL,
    end_location VARCHAR(200) NOT NULL,
    start_latitude DECIMAL(10,7),
    start_longitude DECIMAL(10,7),
    end_latitude DECIMAL(10,7),
    end_longitude DECIMAL(10,7),
    waypoints JSONB DEFAULT '[]',
    route_type_id UUID REFERENCES lookups(id),
    distance_km DECIMAL(10,2),
    estimated_duration_min INT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
)

-- P0: Geofences
geofences (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    name VARCHAR(100) NOT NULL,
    location_type_id UUID REFERENCES lookups(id),
    address VARCHAR(200),
    latitude DECIMAL(10,7) NOT NULL,
    longitude DECIMAL(10,7) NOT NULL,
    radius_meters DECIMAL(10,2) NOT NULL,
    color VARCHAR(20) DEFAULT 'Blue',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
)

-- P0: Subscriptions
subscriptions (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    package_name VARCHAR(100) NOT NULL,
    subscription_from DATE NOT NULL,
    subscription_to DATE NOT NULL,
    invoice_no VARCHAR(50) NOT NULL,
    invoice_date DATE NOT NULL,
    payment_mode_id UUID REFERENCES lookups(id),
    remark TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW()
)

-- P1: Form Role Mapping (RBAC)
form_role_mappings (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    role_id UUID NOT NULL REFERENCES roles(id),
    form_id UUID NOT NULL REFERENCES form_masters(id),
    can_view BOOLEAN DEFAULT false,
    can_add BOOLEAN DEFAULT false,
    can_edit BOOLEAN DEFAULT false,
    can_delete BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, role_id, form_id)
)

-- P1: Form Company Mapping
form_company_mappings (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    form_id UUID NOT NULL REFERENCES form_masters(id),
    is_enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, form_id)
)

-- P1: Form Column Configuration
form_column_configs (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    form_id UUID NOT NULL REFERENCES form_masters(id),
    column_name VARCHAR(100) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    sort_order INT DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW()
)

-- P1: Notifications
notifications (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    user_id UUID NOT NULL REFERENCES users(id),
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    type VARCHAR(50) NOT NULL,  -- info, warning, error, success
    is_read BOOLEAN DEFAULT false,
    link VARCHAR(500),
    created_at TIMESTAMPTZ DEFAULT NOW()
)
```

---

## E. API PLAN

### E1. Lookups API (P0)

| Endpoint | Method | Request | Response | Auth | Validation |
|----------|--------|---------|----------|------|------------|
| `/api/v1/lookups` | GET | `?category=Country&parentId=&search=&activeOnly=true` | `[{id, category, code, label, sortOrder, isActive}]` | ✅ | — |
| `/api/v1/lookups/{id}` | GET | — | `{id, category, code, label, parentId, sortOrder, isActive, metadata}` | ✅ | — |
| `/api/v1/lookups` | POST | `{category, code, label, parentId?, sortOrder?, metadata?}` | `{id, message}` | ✅ Admin | Required: category, code, label |
| `/api/v1/lookups/{id}` | PUT | `{code?, label?, sortOrder?, isActive?, metadata?}` | `{message}` | ✅ Admin | — |
| `/api/v1/lookups/{id}` | DELETE | — | `{message}` | ✅ Admin | Soft delete (isActive=false) |
| `/api/v1/lookups/bulk` | POST | `{items: [{category, code, label, ...}]}` | `{count, message}` | ✅ Admin | Bulk import |

### E2. Clients API (P0)

| Endpoint | Method | Request | Response | Auth | Pagination |
|----------|--------|---------|----------|------|------------|
| `/api/v1/clients` | GET | `?page=1&pageSize=25&search=&status=` | `{items, totalCount}` | ✅ | ✅ |
| `/api/v1/clients/{id}` | GET | — | Full client object | ✅ | — |
| `/api/v1/clients` | POST | Full client form body | `{id, message}` | ✅ | — |
| `/api/v1/clients/{id}` | PUT | Partial update | `{message}` | ✅ | — |
| `/api/v1/clients/{id}` | DELETE | — | `{message}` | ✅ Admin | Soft delete |

### E3. Form Masters API (P0)

| Endpoint | Method | Request | Response | Auth |
|----------|--------|---------|----------|------|
| `/api/v1/forms` | GET | `?search=&activeOnly=` | `[{id, formName, controllerName, ...}]` | ✅ |
| `/api/v1/forms/{id}` | GET | — | Full form object | ✅ |
| `/api/v1/forms` | POST | `{formName, controllerName, actionName, ...}` | `{id, message}` | ✅ Admin |
| `/api/v1/forms/{id}` | PUT | Partial update | `{message}` | ✅ Admin |
| `/api/v1/forms/{id}` | DELETE | — | `{message}` | ✅ Admin |

### E4. Routes API (P0)

| Endpoint | Method | Request | Response | Auth |
|----------|--------|---------|----------|------|
| `/api/v1/routes` | GET | `?page=1&pageSize=25&search=` | `{items, totalCount}` | ✅ |
| `/api/v1/routes/{id}` | GET | — | Full route with waypoints | ✅ |
| `/api/v1/routes` | POST | `{routeName, startLocation, endLocation, ...}` | `{id, message}` | ✅ |
| `/api/v1/routes/{id}` | PUT | Partial update | `{message}` | ✅ |
| `/api/v1/routes/{id}` | DELETE | — | `{message}` | ✅ Admin |

### E5. Geofences API (P0)

| Endpoint | Method | Request | Response | Auth |
|----------|--------|---------|----------|------|
| `/api/v1/geofences` | GET | `?page=1&pageSize=25&search=` | `{items, totalCount}` | ✅ |
| `/api/v1/geofences/{id}` | GET | — | Full geofence object | ✅ |
| `/api/v1/geofences` | POST | `{name, latitude, longitude, radiusMeters, ...}` | `{id, message}` | ✅ |
| `/api/v1/geofences/{id}` | PUT | Partial update | `{message}` | ✅ |
| `/api/v1/geofences/{id}` | DELETE | — | `{message}` | ✅ Admin |

### E6. Subscriptions API (P0)

| Endpoint | Method | Request | Response | Auth |
|----------|--------|---------|----------|------|
| `/api/v1/subscriptions` | GET | `?tenantId=` | Subscription list | ✅ |
| `/api/v1/subscriptions/{id}` | GET | — | Full subscription | ✅ |
| `/api/v1/subscriptions` | POST | `{packageName, subscriptionFrom, ...}` | `{id, message}` | ✅ Admin |
| `/api/v1/subscriptions/{id}` | PUT | Partial update | `{message}` | ✅ Admin |

### E7. Form Role Mapping API (P1)

| Endpoint | Method | Request | Response | Auth |
|----------|--------|---------|----------|------|
| `/api/v1/rbac/role-forms` | GET | `?roleId=&formCategory=` | Matrix of form→rights | ✅ Admin |
| `/api/v1/rbac/role-forms` | POST | `{roleId, formId, canView, canAdd, canEdit, canDelete}` | `{message}` | ✅ Admin |
| `/api/v1/rbac/role-forms/bulk` | POST | `{roleId, mappings: [{formId, ...}]}` | `{message}` | ✅ Admin |

### E8. Audit Log API (P1)

| Endpoint | Method | Request | Response | Auth |
|----------|--------|---------|----------|------|
| `/api/v1/audit` | GET | `?entityType=&entityId=&userId=&from=&to=&page=` | Paginated audit trail | ✅ Admin |

### E9. Notifications API (P1)

| Endpoint | Method | Request | Response | Auth |
|----------|--------|---------|----------|------|
| `/api/v1/notifications` | GET | `?unreadOnly=&page=` | `{items, unreadCount}` | ✅ |
| `/api/v1/notifications/{id}/read` | POST | — | `{message}` | ✅ |
| `/api/v1/notifications/read-all` | POST | — | `{message}` | ✅ |

---

## F. FRONTEND PLAN

### F1. Lookups Management Page

| Property | Value |
|----------|-------|
| **Route** | `settings/lookups` |
| **Purpose** | Manage all dropdown values across the system |
| **Components** | Category sidebar, CRUD table, Add/Edit modal with two-column form |
| **API** | `/api/v1/lookups` |
| **CRUD** | List (grouped by category), Add, Edit, Deactivate |
| **Permissions** | Settings → Lookups: View/Add/Edit/Delete |
| **Empty state** | "No lookup values found for this category" |
| **Features** | Category filter tabs, drag-to-reorder, bulk import, search |

### F2. Client Master Page

| Property | Value |
|----------|-------|
| **Route** | `clients/management` |
| **Purpose** | Manage client/consignee master data |
| **Components** | Table + two-column form (matching reference screenshots) |
| **API** | `/api/v1/clients` |
| **CRUD** | Full — list, create, edit, soft-delete |
| **Form fields** | Company, Parent Client, Client Name, Client Code, Address (full postal), Billing section, Contact details, GST/PAN/CIN, Consignee Category, Contract Signed, Active |
| **Permissions** | Client Management: View/Add/Edit/Delete |

### F3. Form Master Page

| Property | Value |
|----------|-------|
| **Route** | `settings/forms` |
| **Purpose** | Register system forms for RBAC configuration |
| **Components** | Table + two-column form |
| **API** | `/api/v1/forms` |
| **CRUD** | Full — list, create, edit, deactivate |
| **Form fields** | Form Name, Controller Name, Action Name, Class Name, Parent Form, Area Name, Platform (Web/Mobile/Both), Active |

### F4. Route Management Page

| Property | Value |
|----------|-------|
| **Route** | `fleet/routes` |
| **Purpose** | Create and manage transport routes |
| **Components** | Tabs (Create Route / Route List), Form, Leaflet map, Table |
| **API** | `/api/v1/routes` |
| **CRUD** | Full — create with map, list, edit, delete |
| **Form fields** | Start/End Location, Waypoints, Route Name, Company, Route Type, Distance |

### F5. Geofence Management Page

| Property | Value |
|----------|-------|
| **Route** | `fleet/geofences` |
| **Purpose** | Define geofenced areas with map visualization |
| **Components** | Tabs (Create / List / Bulk Upload), Form, Leaflet map with circle, Table |
| **API** | `/api/v1/geofences` |
| **CRUD** | Full — create with map, list, edit, delete |
| **Form fields** | Name, Company, Color, Address/LatLong search, Location Type, Radius |

### F6. Subscription Page

| Property | Value |
|----------|-------|
| **Route** | `settings/subscription` |
| **Purpose** | Track company subscriptions and payments |
| **Components** | Two-column form + subscription history table |
| **API** | `/api/v1/subscriptions` |
| **CRUD** | Full — create, edit, list history |
| **Form fields** | Company, Package, Subscription From/To, Invoice No/Date, Payment Mode, Remark |

### F7. Role & Permission Mapping Page

| Property | Value |
|----------|-------|
| **Route** | `settings/role-permissions` |
| **Purpose** | Map roles to form access rights (View/Add/Edit/Delete) |
| **Components** | Filter dropdowns (Mapping For, Company, Role, Form) + permission matrix table |
| **API** | `/api/v1/rbac/role-forms` |
| **CRUD** | Read/Update matrix — checkboxes for All/View/Add/Edit/Delete per menu row |

### F8. Form Company Mapping Page

| Property | Value |
|----------|-------|
| **Route** | `settings/form-company-mapping` |
| **Purpose** | Enable/disable forms per company |
| **Components** | Company + Form dropdowns + permission matrix table |
| **API** | `/api/v1/rbac/company-forms` |

### F9. Form Column Configuration Page

| Property | Value |
|----------|-------|
| **Route** | `settings/form-columns` |
| **Purpose** | Configure which columns appear per form per company |
| **Components** | Company/Client/Transporter/Form dropdowns + column toggle table |
| **API** | `/api/v1/rbac/column-configs` |

### F10. Notification Center Page

| Property | Value |
|----------|-------|
| **Route** | `notifications` |
| **Purpose** | View and manage in-app notifications |
| **Components** | Notification list with read/unread, mark all read, type badges |
| **API** | `/api/v1/notifications` |

### F11. Audit Trail Page

| Property | Value |
|----------|-------|
| **Route** | `settings/audit-trail` |
| **Purpose** | View system change history |
| **Components** | Filterable table (entity type, user, date range) with expandable old/new values |
| **API** | `/api/v1/audit` |

---

## G. IMPLEMENTATION ORDER

### P0 — Foundation (MUST HAVE FIRST)
1. ✅ Lookup entity + API + seed data (Country/State/City/VehicleType/etc.)
2. ✅ Client entity + API + full CRUD
3. ✅ FormMaster entity + API + full CRUD
4. ✅ Route entity + API + full CRUD with map
5. ✅ Geofence entity + API + full CRUD with map
6. ✅ Subscription entity + API + full CRUD
7. ✅ Update seed data with lookup values for 3 demo tenants

### P1 — Core Productization
8. ✅ FormRoleMapping entity + API (permission matrix)
9. ✅ FormCompanyMapping entity + API
10. ✅ FormColumnConfig entity + API
11. ✅ Notification entity + API
12. ✅ AuditLog middleware (auto-capture changes)
13. ✅ Dynamic navigation from DB

### P2 — Advanced Configuration
14. Custom Fields engine
15. Workflow engine
16. Business Rules engine
17. Form Builder
18. Dashboard configurator

### P3 — Nice-to-Have
19. Report templates
20. Export/Import
21. Multi-language
22. White-label theming

---

## H. ACCEPTANCE CRITERIA

### H1. Lookup Management
1. Admin creates a new lookup category "VehicleType" with values "Truck", "Van", "Bike"
2. API returns all VehicleType lookups
3. Vehicle create form dropdown shows Truck, Van, Bike
4. Admin deactivates "Bike" — still visible in existing records but not in new-record dropdowns
5. Admin reorders items — new sort order persists
6. Change appears in audit logs
7. Non-admin users can view but not modify lookups

### H2. Client Master
1. Admin creates a new Client "ABC Logistics" with full address
2. API returns the client in the list with pagination
3. Client form shows cascading Country→State→City dropdowns
4. "Billing address same as address" checkbox auto-fills billing fields
5. Admin edits client contact info — changes persist
6. Admin deactivates client — hidden from active dropdowns but visible in records
7. Search filters clients by name, code, email

### H3. Route Management
1. Admin creates route "Delhi→Mumbai" with waypoints
2. Map shows route path between locations
3. Route appears in Route List table
4. Admin edits waypoints — map updates
5. Admin deletes route — removed from list
6. Distance auto-calculated from coordinates

### H4. Geofence Management
1. Admin creates geofence "Warehouse Zone" with 500m radius
2. Map shows circle overlay at specified location
3. Color matches selection (Blue, Red, Green, etc.)
4. Geofence appears in Geofence List
5. Admin edits radius — circle resizes on map
6. Bulk upload creates multiple geofences from CSV

### H5. Role-Form Permission Mapping
1. Admin selects Role="Driver" and Form="Dashboard"
2. Matrix shows all menus with All/View/Add/Edit/Delete checkboxes
3. Admin checks "View" for "AI Safety Dashboard"
4. Driver user logs in — can see AI Safety Dashboard in sidebar
5. Driver tries to access Add form — gets 403
6. Admin unchecks all permissions — menu hidden from sidebar
