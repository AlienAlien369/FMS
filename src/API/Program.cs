using FMS.API;
using FMS.API.Services;
using FMS.Application;
using FMS.Application.Common.Interfaces;
using FMS.Infrastructure;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Parse DATABASE_URL BEFORE DI registrations
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
        var queryParams = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var param in uri.Query.TrimStart('?').Split('&'))
            {
                var parts = param.Split('=');
                if (parts.Length == 2) queryParams[parts[0]] = Uri.UnescapeDataString(parts[1]);
            }
        }
        if (!queryParams.ContainsKey("sslmode")) queryParams["sslmode"] = "require";
        var connStr = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode={queryParams["sslmode"]};Trust Server Certificate=true";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;
        Console.WriteLine($"[DB] Parsed: Host={host}, Database={database}, User={user}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB] URI parse failed: {ex.Message}");
        if (!databaseUrl.Contains("sslmode")) databaseUrl += databaseUrl.Contains("?") ? "&sslmode=require" : "?sslmode=require";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;
    }
}
else
{
    Console.WriteLine("[DB] No DATABASE_URL found, using config defaults");
}

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Configuration["Jwt:Key"] = jwtSecret;
    builder.Configuration["Jwt:Issuer"] = "FMS";
    builder.Configuration["Jwt:Audience"] = "FMS";
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Database schema created/verified");

        // Create missing tables for existing DBs
        // EF Core uses PascalCase quoted identifiers: "Lookups", "Id", "Category" etc.
        // Column names must match EF Core's property names exactly.
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();

        // Helper to produce a double-quoted identifier
        string Q(string name) => $"\"{name}\"";

        var tableStatements = new[]
        {
            $"CREATE TABLE IF NOT EXISTS {Q("Lookups")} ({Q("Id")} UUID PRIMARY KEY, {Q("Category")} VARCHAR(50) NOT NULL, {Q("ParentId")} UUID, {Q("Code")} VARCHAR(20) NOT NULL, {Q("Label")} VARCHAR(100) NOT NULL, {Q("SortOrder")} INTEGER DEFAULT 0, {Q("IsActive")} BOOLEAN DEFAULT true, {Q("Metadata")} JSONB DEFAULT '{{}}', {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("Clients")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("ParentClientId")} UUID, {Q("CompanyName")} VARCHAR(200), {Q("ClientName")} VARCHAR(200) NOT NULL, {Q("ClientCode")} VARCHAR(50) NOT NULL, {Q("Address")} TEXT, {Q("PinCode")} VARCHAR(20), {Q("CountryId")} UUID, {Q("StateId")} UUID, {Q("CityId")} UUID, {Q("Latitude")} DECIMAL(10,7), {Q("Longitude")} DECIMAL(10,7), {Q("BillingAddressSame")} BOOLEAN DEFAULT false, {Q("BillingAddress")} TEXT, {Q("BillingPinCode")} VARCHAR(20), {Q("BillingCountryId")} UUID, {Q("BillingStateId")} UUID, {Q("BillingCityId")} UUID, {Q("CompanyPhone")} VARCHAR(20), {Q("ContactPerson")} VARCHAR(100), {Q("ContactNo")} VARCHAR(20), {Q("AltContactNo")} VARCHAR(20), {Q("ContactEmail")} VARCHAR(100), {Q("MobileNo")} VARCHAR(20), {Q("EmailId")} VARCHAR(100), {Q("AltEmailId")} VARCHAR(100), {Q("PanNo")} VARCHAR(20), {Q("GstNo")} VARCHAR(30), {Q("CinNo")} VARCHAR(30), {Q("ConsigneeCategoryId")} UUID, {Q("IsContractSigned")} BOOLEAN DEFAULT false, {Q("IsActive")} BOOLEAN DEFAULT true, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW(), {Q("UpdatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("FormMasters")} ({Q("Id")} UUID PRIMARY KEY, {Q("FormName")} VARCHAR(100) NOT NULL, {Q("ControllerName")} VARCHAR(100) NOT NULL, {Q("ActionName")} VARCHAR(100) NOT NULL, {Q("ClassName")} VARCHAR(100), {Q("ParentFormId")} UUID, {Q("AreaName")} VARCHAR(50), {Q("Platform")} VARCHAR(20) DEFAULT 'Web', {Q("IsActive")} BOOLEAN DEFAULT true, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("Routes")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("RouteName")} VARCHAR(100) NOT NULL, {Q("StartLocation")} VARCHAR(200) NOT NULL, {Q("EndLocation")} VARCHAR(200) NOT NULL, {Q("StartLatitude")} DECIMAL(10,7), {Q("StartLongitude")} DECIMAL(10,7), {Q("EndLatitude")} DECIMAL(10,7), {Q("EndLongitude")} DECIMAL(10,7), {Q("Waypoints")} JSONB DEFAULT '[]', {Q("RouteTypeId")} UUID, {Q("DistanceKm")} DECIMAL(10,2), {Q("EstimatedDurationMin")} INTEGER, {Q("IsActive")} BOOLEAN DEFAULT true, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW(), {Q("UpdatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("Geofences")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("Name")} VARCHAR(100) NOT NULL, {Q("LocationTypeId")} UUID, {Q("Address")} VARCHAR(200), {Q("Latitude")} DECIMAL(10,7) NOT NULL, {Q("Longitude")} DECIMAL(10,7) NOT NULL, {Q("RadiusMeters")} DECIMAL(10,2) NOT NULL, {Q("Color")} VARCHAR(20) DEFAULT 'Blue', {Q("IsActive")} BOOLEAN DEFAULT true, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW(), {Q("UpdatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("Subscriptions")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("PackageName")} VARCHAR(100) NOT NULL, {Q("SubscriptionFrom")} DATE NOT NULL, {Q("SubscriptionTo")} DATE NOT NULL, {Q("InvoiceNo")} VARCHAR(50) NOT NULL, {Q("InvoiceDate")} DATE NOT NULL, {Q("PaymentModeId")} UUID, {Q("Remark")} TEXT, {Q("IsActive")} BOOLEAN DEFAULT true, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("FormRoleMappings")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("RoleId")} UUID NOT NULL, {Q("FormId")} UUID NOT NULL, {Q("CanView")} BOOLEAN DEFAULT false, {Q("CanAdd")} BOOLEAN DEFAULT false, {Q("CanEdit")} BOOLEAN DEFAULT false, {Q("CanDelete")} BOOLEAN DEFAULT false, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("FormCompanyMappings")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("FormId")} UUID NOT NULL, {Q("IsEnabled")} BOOLEAN DEFAULT true, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("FormColumnConfigs")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("FormId")} UUID NOT NULL, {Q("ColumnName")} VARCHAR(100) NOT NULL, {Q("DisplayName")} VARCHAR(100) NOT NULL, {Q("IsActive")} BOOLEAN DEFAULT true, {Q("SortOrder")} INTEGER DEFAULT 0, {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW())",
            $"CREATE TABLE IF NOT EXISTS {Q("Notifications")} ({Q("Id")} UUID PRIMARY KEY, {Q("TenantId")} UUID NOT NULL, {Q("UserId")} UUID NOT NULL, {Q("Title")} VARCHAR(200) NOT NULL, {Q("Message")} TEXT NOT NULL, {Q("Type")} VARCHAR(50) NOT NULL, {Q("IsRead")} BOOLEAN DEFAULT false, {Q("Link")} VARCHAR(500), {Q("CreatedAt")} TIMESTAMPTZ DEFAULT NOW())",
        };

        Console.WriteLine("[DB] Creating new tables with PascalCase columns...");
        foreach (var sql in tableStatements)
        {
            try
            {
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Table error: {ex.Message.Split('\n')[0]}");
            }
        }

        // Fix JSONB columns that should be TEXT (EF Core converters serialize to string)
        var fixColumns = new[] {
            $"ALTER TABLE {Q("Lookups")} ALTER COLUMN {Q("Metadata")} TYPE TEXT USING {Q("Metadata")}::text",
            $"ALTER TABLE {Q("Routes")} ALTER COLUMN {Q("Waypoints")} TYPE TEXT USING {Q("Waypoints")}::text",
        };
        foreach (var sql in fixColumns)
        {
            try { cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync(); }
            catch { }
        }

        // Add self-referencing foreign key for Lookups after table exists
        try
        {
            cmd.CommandText = $"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_lookups_parent') THEN ALTER TABLE {Q("Lookups")} ADD CONSTRAINT fk_lookups_parent FOREIGN KEY ({Q("ParentId")}) REFERENCES {Q("Lookups")}({Q("Id")}); END IF; END $$;";
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }

        // Add indexes for new tables
        var indexStatements = new[]
        {
            $"CREATE INDEX IF NOT EXISTS idx_lookups_category ON {Q("Lookups")}({Q("Category")})",
            $"CREATE UNIQUE INDEX IF NOT EXISTS idx_lookups_cat_code ON {Q("Lookups")}({Q("Category")}, {Q("Code")})",
            $"CREATE INDEX IF NOT EXISTS idx_clients_tenant ON {Q("Clients")}({Q("TenantId")})",
            $"CREATE UNIQUE INDEX IF NOT EXISTS idx_clients_tenant_code ON {Q("Clients")}({Q("TenantId")}, {Q("ClientCode")})",
            $"CREATE UNIQUE INDEX IF NOT EXISTS idx_forms_name ON {Q("FormMasters")}({Q("FormName")})",
            $"CREATE INDEX IF NOT EXISTS idx_routes_tenant ON {Q("Routes")}({Q("TenantId")})",
            $"CREATE INDEX IF NOT EXISTS idx_geofences_tenant ON {Q("Geofences")}({Q("TenantId")})",
            $"CREATE INDEX IF NOT EXISTS idx_subscriptions_tenant ON {Q("Subscriptions")}({Q("TenantId")})",
            $"CREATE UNIQUE INDEX IF NOT EXISTS idx_frm_tenant_role_form ON {Q("FormRoleMappings")}({Q("TenantId")}, {Q("RoleId")}, {Q("FormId")})",
            $"CREATE UNIQUE INDEX IF NOT EXISTS idx_fcm_tenant_form ON {Q("FormCompanyMappings")}({Q("TenantId")}, {Q("FormId")})",
            $"CREATE INDEX IF NOT EXISTS idx_notifications_user_read ON {Q("Notifications")}({Q("UserId")}, {Q("IsRead")})",
        };

        foreach (var sql in indexStatements)
        {
            try { cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync(); }
            catch { }
        }

        // Widen columns that EF Core created too narrow
        var widenStatements = new[]
        {
            $"ALTER TABLE {Q("Tenants")} ALTER COLUMN {Q("CountryCode")} TYPE VARCHAR(10)",
            $"ALTER TABLE {Q("Tenants")} ALTER COLUMN {Q("Timezone")} TYPE VARCHAR(50)",
            $"ALTER TABLE {Q("Tenants")} ALTER COLUMN {Q("Currency")} TYPE VARCHAR(10)",
            $"ALTER TABLE {Q("Tenants")} ALTER COLUMN {Q("Plan")} TYPE VARCHAR(20)",
            $"ALTER TABLE {Q("Tenants")} ALTER COLUMN {Q("Status")} TYPE VARCHAR(20)",
            $"ALTER TABLE {Q("Tenants")} ALTER COLUMN {Q("DataResidencyRegion")} TYPE VARCHAR(50)",
            $"ALTER TABLE {Q("Vehicles")} ALTER COLUMN {Q("Type")} TYPE VARCHAR(50)",
            $"ALTER TABLE {Q("Vehicles")} ALTER COLUMN {Q("Model")} TYPE VARCHAR(100)",
            $"ALTER TABLE {Q("Vehicles")} ALTER COLUMN {Q("FuelType")} TYPE VARCHAR(30)",
            $"ALTER TABLE {Q("Vehicles")} ALTER COLUMN {Q("Status")} TYPE VARCHAR(30)",
            $"ALTER TABLE {Q("Drivers")} ALTER COLUMN {Q("Status")} TYPE VARCHAR(30)",
            $"ALTER TABLE {Q("Devices")} ALTER COLUMN {Q("Model")} TYPE VARCHAR(100)",
            $"ALTER TABLE {Q("Devices")} ALTER COLUMN {Q("Status")} TYPE VARCHAR(30)",
            $"ALTER TABLE {Q("DeviceVendors")} ALTER COLUMN {Q("Protocol")} TYPE VARCHAR(20)",
            $"ALTER TABLE {Q("DeviceCommands")} ALTER COLUMN {Q("CommandType")} TYPE VARCHAR(50)",
            $"ALTER TABLE {Q("DeviceCommands")} ALTER COLUMN {Q("Status")} TYPE VARCHAR(30)",
            $"ALTER TABLE {Q("Users")} ALTER COLUMN {Q("Email")} TYPE VARCHAR(200)",
        };

        foreach (var sql in widenStatements)
        {
            try { cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync(); }
            catch { }
        }

        await conn.CloseAsync();
        Console.WriteLine("✅ Database schema updated — all tables and indexes created");
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
