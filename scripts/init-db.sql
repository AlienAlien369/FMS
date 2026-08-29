-- FMS Database Initialization Script
-- Creates RLS policies for multi-tenant isolation

-- Enable PostGIS extension for geospatial queries
CREATE EXTENSION IF NOT EXISTS "postgis";

-- Function to set current tenant for RLS
CREATE OR REPLACE FUNCTION set_current_tenant(tenant_uuid UUID)
RETURNS VOID AS $$
BEGIN
    PERFORM set_config('app.current_tenant', tenant_uuid::TEXT, true);
END;
$$ LANGUAGE plpgsql;

-- ==========================================
-- RLS POLICIES (Applied to all tenant-scoped tables)
-- ==========================================

-- Tenants table (not tenant-scoped, but admin-only)
-- Users table
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_users ON users
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Roles table
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_roles ON roles
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Features table
ALTER TABLE features ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_features ON features
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Vehicles table
ALTER TABLE vehicles ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_vehicles ON vehicles
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Drivers table
ALTER TABLE drivers ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_drivers ON drivers
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Devices table
ALTER TABLE devices ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_devices ON devices
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Device Commands table
ALTER TABLE device_commands ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_device_commands ON device_commands
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- Audit Logs table
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_audit_logs ON audit_logs
    USING (tenant_id = current_setting('app.current_tenant')::UUID);

-- User Preferences table (user-scoped, not tenant-scoped directly)
ALTER TABLE user_preferences ENABLE ROW LEVEL SECURITY;
CREATE POLICY user_isolation_preferences ON user_preferences
    USING (user_id = current_setting('app.current_user_id')::UUID);

-- ==========================================
-- INDEXES (Performance optimization)
-- ==========================================
CREATE INDEX IF NOT EXISTS idx_users_tenant_email ON users(tenant_id, email);
CREATE INDEX IF NOT EXISTS idx_roles_tenant ON roles(tenant_id);
CREATE INDEX IF NOT EXISTS idx_features_tenant ON features(tenant_id);
CREATE INDEX IF NOT EXISTS idx_vehicles_tenant_status ON vehicles(tenant_id, status);
CREATE INDEX IF NOT EXISTS idx_drivers_tenant ON drivers(tenant_id);
CREATE INDEX IF NOT EXISTS idx_devices_tenant_status ON devices(tenant_id, status);
CREATE INDEX IF NOT EXISTS idx_devices_imei ON devices(imei);
CREATE INDEX IF NOT EXISTS idx_device_commands_device ON device_commands(device_id, status);
CREATE INDEX IF NOT EXISTS idx_audit_logs_tenant_date ON audit_logs(tenant_id, created_at);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
