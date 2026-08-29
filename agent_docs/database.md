# FMS Database Schema Reference

## PostgreSQL (Structured Data)

### Tenant Registry
```sql
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    subdomain VARCHAR(100) UNIQUE NOT NULL,
    custom_domain VARCHAR(255),
    country_code VARCHAR(2) NOT NULL DEFAULT 'US',
    timezone VARCHAR(50) NOT NULL DEFAULT 'UTC',
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    plan VARCHAR(20) NOT NULL DEFAULT 'basic', -- basic, pro, enterprise
    status VARCHAR(20) NOT NULL DEFAULT 'trial', -- active, suspended, trial
    data_residency_region VARCHAR(50) NOT NULL DEFAULT 'us-east-1',
    settings JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_tenants_subdomain ON tenants(subdomain);
CREATE INDEX idx_tenants_status ON tenants(status);
```

### Users & RBAC
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    role_id UUID,
    preferences JSONB DEFAULT '{}',
    mfa_enabled BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    last_login TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(tenant_id, email)
);

CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    permissions JSONB NOT NULL DEFAULT '[]',
    is_system_role BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### Feature Registry
```sql
CREATE TABLE features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    module VARCHAR(50) NOT NULL,
    feature VARCHAR(100) NOT NULL,
    enabled BOOLEAN NOT NULL DEFAULT false,
    config JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(tenant_id, module, feature)
);
```

### Vehicles & Drivers
```sql
CREATE TABLE vehicles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    vehicle_number VARCHAR(50) NOT NULL,
    type VARCHAR(50),
    model VARCHAR(100),
    year INT,
    fuel_type VARCHAR(20),
    gps_device_id VARCHAR(100),
    status VARCHAR(20) DEFAULT 'active',
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE drivers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    license_number VARCHAR(100),
    license_expiry DATE,
    phone VARCHAR(20),
    behavior_score DECIMAL(4,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'active',
    documents JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### User Preferences (Dynamic Tables/Dashboards)
```sql
CREATE TABLE user_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    page VARCHAR(100) NOT NULL,
    preference_type VARCHAR(50) NOT NULL, -- "table-columns", "dashboard-layout", "form-config"
    config JSONB NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(user_id, page, preference_type)
);
```

### Audit Logs
```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    user_id UUID,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID,
    old_value JSONB,
    new_value JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_audit_logs_tenant_date ON audit_logs(tenant_id, created_at);
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
```

### Device Adapter Framework
```sql
CREATE TABLE device_vendors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    protocol VARCHAR(20) NOT NULL,
    default_port INT,
    supports_video BOOLEAN DEFAULT false,
    supports_fuel BOOLEAN DEFAULT false,
    supports_temperature BOOLEAN DEFAULT false,
    supports_can_bus BOOLEAN DEFAULT false,
    schema_config JSONB NOT NULL,
    adapter_version VARCHAR(20) DEFAULT '1.0',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    vendor_id UUID NOT NULL REFERENCES device_vendors(id),
    imei VARCHAR(50),
    serial_number VARCHAR(100),
    model VARCHAR(100),
    firmware_version VARCHAR(50),
    vehicle_id UUID REFERENCES vehicles(id),
    driver_id UUID REFERENCES drivers(id),
    status VARCHAR(20) DEFAULT 'active',
    config JSONB DEFAULT '{}',
    last_seen TIMESTAMP WITH TIME ZONE,
    last_speed DECIMAL(5,2),
    signal_strength INT,
    battery_level INT,
    installed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE device_commands (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    device_id UUID NOT NULL REFERENCES devices(id),
    command_type VARCHAR(50) NOT NULL,
    payload JSONB,
    status VARCHAR(20) DEFAULT 'pending',
    sent_at TIMESTAMP WITH TIME ZONE,
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    response_payload JSONB,
    error_message TEXT,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE TABLE firmware_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES device_vendors(id),
    model VARCHAR(100),
    version VARCHAR(50),
    release_notes TEXT,
    file_url VARCHAR(500),
    is_mandatory BOOLEAN DEFAULT false,
    rollout_percentage INT DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

### RLS Policy Template (Apply to ALL tenant-scoped tables)
```sql
ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_{table} ON {table}
    USING (tenant_id = current_setting('app.current_tenant')::UUID);
```

## MongoDB Collections

### Raw Telemetry (Time-Series)
```javascript
db.createCollection("telemetry", {
  timeseries: {
    timeField: "timestamp",
    metaField: "meta",
    granularity: "seconds"
  }
});

// Indexes
db.telemetry.createIndex({ "meta.tenantId": 1, "meta.deviceId": 1, timestamp: -1 });
db.telemetry.createIndex({ "location": "2dsphere" });
```

### Trips
```javascript
db.trips.createIndex({ tenantId: 1, vehicleId: 1, startTime: -1 });
db.trips.createIndex({ "route.location": "2dsphere" });
```

### Alerts
```javascript
db.alerts.createIndex({ tenantId: 1, alertType: 1, timestamp: -1 });
db.alerts.createIndex({ tenantId: 1, resolved: 1, severity: 1 });
```

### Video Events
```javascript
db.video_events.createIndex({ tenantId: 1, deviceId: 1, timestamp: -1 });
```
