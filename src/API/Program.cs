using FMS.API;
using FMS.API.Services;
using FMS.Application;
using FMS.Application.Common.Interfaces;
using FMS.Infrastructure;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// 1. Parse DATABASE_URL BEFORE DI registrations (critical!)
//    AddInfrastructure registers FmsDbContext with the connection
//    string at registration time, so the value must be set first.
// ─────────────────────────────────────────────────────────────

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine($"[DB] DATABASE_URL found (length={databaseUrl.Length})");

    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        // Parse query params
        var queryParams = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var param in uri.Query.TrimStart('?').Split('&'))
            {
                var parts = param.Split('=');
                if (parts.Length == 2)
                    queryParams[parts[0]] = Uri.UnescapeDataString(parts[1]);
            }
        }

        if (!queryParams.ContainsKey("sslmode"))
            queryParams["sslmode"] = "require";

        var connStr = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode={queryParams["sslmode"]};Trust Server Certificate=true";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;
        Console.WriteLine($"[DB] Parsed connection: Host={host}, Port={port}, Database={database}, User={user}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB] URI parse failed: {ex.Message}");
        // Use raw URL as fallback
        if (!databaseUrl.Contains("sslmode"))
            databaseUrl += databaseUrl.Contains("?") ? "&sslmode=require" : "?sslmode=require";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;
    }
}
else
{
    Console.WriteLine("[DB] No DATABASE_URL found, using config defaults");
}

// JWT Secret from env var
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Configuration["Jwt:Key"] = jwtSecret;
    builder.Configuration["Jwt:Issuer"] = "FMS";
    builder.Configuration["Jwt:Audience"] = "FMS";
}

// ─────────────────────────────────────────────────────────────
// 2. Register services (now reads the parsed connection string)
// ─────────────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("FmsCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201",
                "https://fms-sage-kappa.vercel.app",
                "https://fms-web-landing.vercel.app",
                "https://fms-web-landing-lakshyas-projects-c97e54f6.vercel.app",
                "https://fms-web-lakshyas-projects-c97e54f6.vercel.app",
                "https://fms-4wpzsv4ub-lakshyas-projects-c97e54f6.vercel.app",
                "https://fms-web-uat.vercel.app",
                "https://fms-admin-uat.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FMS Fleet Management API", Version = "v1" });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

// Auto-migrate database and seed sample data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FmsDbContext>();
        
        // Try EnsureCreated first (works for new DBs)
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Database schema created/verified");
        
        // For existing DBs, create missing tables via raw SQL
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        // Helper: escape double quotes for C# verbatim strings
        string Q(string name) => name.Replace("\"", "\"\"");
        string T(string t) => $"\"{Q(t)}\"";
        
        cmd.CommandText = $@"
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Lookups') THEN
    CREATE TABLE {T("Lookups")} (
      id UUID PRIMARY KEY, category VARCHAR(50) NOT NULL, parent_id UUID REFERENCES {T("Lookups")}(id),
      code VARCHAR(20) NOT NULL, label VARCHAR(100) NOT NULL, sort_order INT DEFAULT 0,
      is_active BOOLEAN DEFAULT true, metadata JSONB DEFAULT '{{}}', created_at TIMESTAMPTZ DEFAULT NOW());
    CREATE INDEX idx_lookups_category ON {T("Lookups")}(category);
    CREATE INDEX idx_lookups_parent ON {T("Lookups")}(parent_id);
    CREATE UNIQUE INDEX idx_lookups_cat_code ON {T("Lookups")}(category, code);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Clients') THEN
    CREATE TABLE {T("Clients")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      parent_client_id UUID, company_name VARCHAR(200), client_name VARCHAR(200) NOT NULL,
      client_code VARCHAR(50) NOT NULL, address TEXT, pin_code VARCHAR(20),
      country_id UUID, state_id UUID, city_id UUID,
      latitude DECIMAL(10,7), longitude DECIMAL(10,7),
      billing_address_same BOOLEAN DEFAULT false, billing_address TEXT, billing_pin_code VARCHAR(20),
      billing_country_id UUID, billing_state_id UUID, billing_city_id UUID,
      company_phone VARCHAR(20), contact_person VARCHAR(100), contact_no VARCHAR(20),
      alt_contact_no VARCHAR(20), contact_email VARCHAR(100), mobile_no VARCHAR(20),
      email_id VARCHAR(100), alt_email_id VARCHAR(100), pan_no VARCHAR(20),
      gst_no VARCHAR(30), cin_no VARCHAR(30), consignee_category_id UUID,
      is_contract_signed BOOLEAN DEFAULT false, is_active BOOLEAN DEFAULT true,
      created_at TIMESTAMPTZ DEFAULT NOW(), updated_at TIMESTAMPTZ DEFAULT NOW());
    CREATE INDEX idx_clients_tenant ON {T("Clients")}(tenant_id);
    CREATE UNIQUE INDEX idx_clients_tenant_code ON {T("Clients")}(tenant_id, client_code);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'FormMasters') THEN
    CREATE TABLE {T("FormMasters")} (
      id UUID PRIMARY KEY, form_name VARCHAR(100) NOT NULL,
      controller_name VARCHAR(100) NOT NULL, action_name VARCHAR(100) NOT NULL,
      class_name VARCHAR(100), parent_form_id UUID, area_name VARCHAR(50),
      platform VARCHAR(20) DEFAULT 'Web', is_active BOOLEAN DEFAULT true,
      created_at TIMESTAMPTZ DEFAULT NOW());
    CREATE UNIQUE INDEX idx_forms_name ON {T("FormMasters")}(form_name);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Routes') THEN
    CREATE TABLE {T("Routes")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      route_name VARCHAR(100) NOT NULL, start_location VARCHAR(200) NOT NULL,
      end_location VARCHAR(200) NOT NULL, start_latitude DECIMAL(10,7), start_longitude DECIMAL(10,7),
      end_latitude DECIMAL(10,7), end_longitude DECIMAL(10,7),
      waypoints JSONB DEFAULT '[]', route_type_id UUID, distance_km DECIMAL(10,2),
      estimated_duration_min INT, is_active BOOLEAN DEFAULT true,
      created_at TIMESTAMPTZ DEFAULT NOW(), updated_at TIMESTAMPTZ DEFAULT NOW());
    CREATE INDEX idx_routes_tenant ON {T("Routes")}(tenant_id);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Geofences') THEN
    CREATE TABLE {T("Geofences")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      name VARCHAR(100) NOT NULL, location_type_id UUID, address VARCHAR(200),
      latitude DECIMAL(10,7) NOT NULL, longitude DECIMAL(10,7) NOT NULL,
      radius_meters DECIMAL(10,2) NOT NULL, color VARCHAR(20) DEFAULT 'Blue',
      is_active BOOLEAN DEFAULT true, created_at TIMESTAMPTZ DEFAULT NOW(),
      updated_at TIMESTAMPTZ DEFAULT NOW());
    CREATE INDEX idx_geofences_tenant ON {T("Geofences")}(tenant_id);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Subscriptions') THEN
    CREATE TABLE {T("Subscriptions")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      package_name VARCHAR(100) NOT NULL, subscription_from DATE NOT NULL,
      subscription_to DATE NOT NULL, invoice_no VARCHAR(50) NOT NULL,
      invoice_date DATE NOT NULL, payment_mode_id UUID, remark TEXT,
      is_active BOOLEAN DEFAULT true, created_at TIMESTAMPTZ DEFAULT NOW());
    CREATE INDEX idx_subscriptions_tenant ON {T("Subscriptions")}(tenant_id);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'FormRoleMappings') THEN
    CREATE TABLE {T("FormRoleMappings")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      role_id UUID NOT NULL, form_id UUID NOT NULL,
      can_view BOOLEAN DEFAULT false, can_add BOOLEAN DEFAULT false,
      can_edit BOOLEAN DEFAULT false, can_delete BOOLEAN DEFAULT false,
      created_at TIMESTAMPTZ DEFAULT NOW());
    CREATE UNIQUE INDEX idx_frm_tenant_role_form ON {T("FormRoleMappings")}(tenant_id, role_id, form_id);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'FormCompanyMappings') THEN
    CREATE TABLE {T("FormCompanyMappings")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      form_id UUID NOT NULL, is_enabled BOOLEAN DEFAULT true,
      created_at TIMESTAMPTZ DEFAULT NOW());
    CREATE UNIQUE INDEX idx_fcm_tenant_form ON {T("FormCompanyMappings")}(tenant_id, form_id);
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'FormColumnConfigs') THEN
    CREATE TABLE {T("FormColumnConfigs")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      form_id UUID NOT NULL, column_name VARCHAR(100) NOT NULL,
      display_name VARCHAR(100) NOT NULL, is_active BOOLEAN DEFAULT true,
      sort_order INT DEFAULT 0, created_at TIMESTAMPTZ DEFAULT NOW());
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Notifications') THEN
    CREATE TABLE {T("Notifications")} (
      id UUID PRIMARY KEY, tenant_id UUID NOT NULL REFERENCES tenants(id),
      user_id UUID NOT NULL REFERENCES users(id), title VARCHAR(200) NOT NULL,
      message TEXT NOT NULL, type VARCHAR(50) NOT NULL, is_read BOOLEAN DEFAULT false,
      link VARCHAR(500), created_at TIMESTAMPTZ DEFAULT NOW());
    CREATE INDEX idx_notifications_user_read ON {T("Notifications")}(user_id, is_read);
  END IF;
END $$;
";
        Console.WriteLine("[DB] Creating new tables...");
        try { await cmd.ExecuteNonQueryAsync(); Console.WriteLine("✅ New tables created"); } catch (Exception ex) { Console.WriteLine($"⚠️ Table creation: {ex.Message}"); }
        
        // Widen existing columns that EF Core created too narrow
        cmd.CommandText = $@"
ALTER TABLE {T("Tenants")} ALTER COLUMN {T("CountryCode")} TYPE VARCHAR(10);
ALTER TABLE {T("Tenants")} ALTER COLUMN {T("Timezone")} TYPE VARCHAR(50);
ALTER TABLE {T("Tenants")} ALTER COLUMN {T("Currency")} TYPE VARCHAR(10);
ALTER TABLE {T("Tenants")} ALTER COLUMN {T("Plan")} TYPE VARCHAR(20);
ALTER TABLE {T("Tenants")} ALTER COLUMN {T("Status")} TYPE VARCHAR(20);
ALTER TABLE {T("Tenants")} ALTER COLUMN {T("DataResidencyRegion")} TYPE VARCHAR(50);
ALTER TABLE {T("Vehicles")} ALTER COLUMN {T("Type")} TYPE VARCHAR(50);
ALTER TABLE {T("Vehicles")} ALTER COLUMN {T("Model")} TYPE VARCHAR(100);
ALTER TABLE {T("Vehicles")} ALTER COLUMN {T("FuelType")} TYPE VARCHAR(30);
ALTER TABLE {T("Vehicles")} ALTER COLUMN {T("Status")} TYPE VARCHAR(30);
ALTER TABLE {T("Drivers")} ALTER COLUMN {T("Status")} TYPE VARCHAR(30);
ALTER TABLE {T("Devices")} ALTER COLUMN {T("Model")} TYPE VARCHAR(100);
ALTER TABLE {T("Devices")} ALTER COLUMN {T("Status")} TYPE VARCHAR(30);
ALTER TABLE {T("DeviceVendors")} ALTER COLUMN {T("Protocol")} TYPE VARCHAR(20);
ALTER TABLE {T("DeviceCommands")} ALTER COLUMN {T("CommandType")} TYPE VARCHAR(50);
ALTER TABLE {T("DeviceCommands")} ALTER COLUMN {T("Status")} TYPE VARCHAR(30);
ALTER TABLE {T("Users")} ALTER COLUMN {T("Email")} TYPE VARCHAR(200);
";
        try { await cmd.ExecuteNonQueryAsync(); Console.WriteLine("✅ Columns widened"); } catch { /* columns already wide enough */ }
        await conn.CloseAsync();
        Console.WriteLine("✅ Database schema updated");
    }

    await SeedData.SeedAsync(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Database/seed error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.WriteLine("   App will continue without database — health check will report unhealthy");
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FMS API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("FmsCors");

// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title = exception?.Message ?? "An error occurred",
            status = statusCode
        });
    });
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Root endpoint for Render health check
app.MapGet("/", () => "FMS Fleet Management API is running 🚛");

Console.WriteLine($"🚀 FMS API starting on {string.Join(", ", builder.Configuration["Urls"] ?? "http://+:5000")}");
Console.WriteLine($"   Environment: {app.Environment.EnvironmentName}");

app.Run();
