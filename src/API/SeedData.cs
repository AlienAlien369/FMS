using FMS.Domain.Entities;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMS.API;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FmsDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Tenants.AnyAsync())
            return;

        Console.WriteLine("[Seed] Seeding sample data for UAT...");

        // ==========================================
        // TENANTS (Multiple companies)
        // ==========================================
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var tenant3Id = Guid.NewGuid();

        var tenants = new List<Tenant>
        {
            new()
            {
                Id = tenant1Id,
                Name = "Acme Logistics Corp",
                Subdomain = "acme-logistics",
                CountryCode = "IN",
                Timezone = "Asia/Kolkata",
                Currency = "INR",
                Plan = "pro",
                Status = "active",
                DataResidencyRegion = "ap-south-1",
                Settings = new Dictionary<string, object>
                {
                    ["branding"] = new Dictionary<string, string>
                    {
                        ["primaryColor"] = "#1e40af",
                        ["secondaryColor"] = "#3b82f6",
                        ["logoUrl"] = "/assets/logos/acme-logo.svg",
                        ["faviconUrl"] = "/assets/favicon.ico"
                    },
                    ["allowedOrigins"] = new[] { "https://acme-logistics.fms-uat.vercel.app" }
                }
            },
            new()
            {
                Id = tenant2Id,
                Name = "SafeRide Taxi Services",
                Subdomain = "saferide-taxi",
                CountryCode = "US",
                Timezone = "America/New_York",
                Currency = "USD",
                Plan = "basic",
                Status = "active",
                DataResidencyRegion = "us-east-1",
                Settings = new Dictionary<string, object>
                {
                    ["branding"] = new Dictionary<string, string>
                    {
                        ["primaryColor"] = "#059669",
                        ["secondaryColor"] = "#10b981",
                        ["logoUrl"] = "/assets/logos/saferide-logo.svg"
                    }
                }
            },
            new()
            {
                Id = tenant3Id,
                Name = "Gulf Mining Group",
                Subdomain = "gulf-mining",
                CountryCode = "SA",
                Timezone = "Asia/Riyadh",
                Currency = "SAR",
                Plan = "enterprise",
                Status = "active",
                DataResidencyRegion = "me-south-1",
                Settings = new Dictionary<string, object>
                {
                    ["branding"] = new Dictionary<string, string>
                    {
                        ["primaryColor"] = "#dc2626",
                        ["secondaryColor"] = "#ef4444",
                        ["logoUrl"] = "/assets/logos/gulf-logo.svg"
                    },
                    ["rtl"] = true
                }
            }
        };

        context.Tenants.AddRange(tenants);
        await context.SaveChangesAsync();

        // ==========================================
        // DEVICE VENDORS (Platform-level)
        // ==========================================
        var vendor1Id = Guid.NewGuid();
        var vendor2Id = Guid.NewGuid();
        var vendor3Id = Guid.NewGuid();

        var vendors = new List<DeviceVendor>
        {
            new()
            {
                Id = vendor1Id,
                Name = "iTriangle Infotech",
                Code = "itriangle",
                Protocol = "tcp",
                DefaultPort = 5001,
                SupportsVideo = false,
                SupportsFuel = true,
                SupportsTemperature = true,
                SchemaConfig = new Dictionary<string, object>
                {
                    ["protocol"] = "tcp",
                    ["payloadFormat"] = "binary",
                    ["fieldMapping"] = new Dictionary<string, string>
                    {
                        ["latitude"] = "$.gps.lat",
                        ["longitude"] = "$.gps.lng",
                        ["speed"] = "$.speed",
                        ["heading"] = "$.direction",
                        ["ignition"] = "$.io.ignition",
                        ["odometer"] = "$.mileage",
                        ["fuelLevel"] = "$.fuel.level"
                    }
                }
            },
            new()
            {
                Id = vendor2Id,
                Name = "Streamax Technology",
                Code = "streamax",
                Protocol = "mqtt",
                DefaultPort = 1883,
                SupportsVideo = true,
                SupportsFuel = false,
                SupportsTemperature = false,
                SchemaConfig = new Dictionary<string, object>
                {
                    ["protocol"] = "mqtt",
                    ["payloadFormat"] = "json",
                    ["fieldMapping"] = new Dictionary<string, string>
                    {
                        ["latitude"] = "$.gps.latitude",
                        ["longitude"] = "$.gps.longitude",
                        ["speed"] = "$.vehicle.speed"
                    },
                    ["videoChannels"] = 4,
                    ["videoResolution"] = "1080p"
                }
            },
            new()
            {
                Id = vendor3Id,
                Name = "Teltonika Telematics",
                Code = "teltonika",
                Protocol = "tcp",
                DefaultPort = 5000,
                SupportsVideo = false,
                SupportsFuel = true,
                SupportsTemperature = true,
                SupportsCanBus = true,
                SchemaConfig = new Dictionary<string, object>
                {
                    ["protocol"] = "tcp",
                    ["payloadFormat"] = "binary",
                    ["parser"] = "teltonika_fmc"
                }
            }
        };

        context.DeviceVendors.AddRange(vendors);
        await context.SaveChangesAsync();

        // ==========================================
        // USERS (Admin for each tenant)
        // ==========================================
        var adminRoleId1 = Guid.NewGuid();
        var adminRoleId2 = Guid.NewGuid();
        var adminRoleId3 = Guid.NewGuid();
        var driverRoleId = Guid.NewGuid();

        var roles = new List<Role>
        {
            new() { Id = adminRoleId1, TenantId = tenant1Id, Name = "Super Admin", Permissions = new List<string> { "command-center:read", "command-center:write", "fleet-intelligence:read", "fleet-intelligence:write", "trip-logistics:read", "trip-logistics:write", "settings:read", "settings:write", "device-iot:read", "device-iot:write" }, IsSystemRole = true },
            new() { Id = adminRoleId2, TenantId = tenant2Id, Name = "Super Admin", Permissions = new List<string> { "command-center:read", "command-center:write", "fleet-intelligence:read", "fleet-intelligence:write", "settings:read", "settings:write" }, IsSystemRole = true },
            new() { Id = adminRoleId3, TenantId = tenant3Id, Name = "Super Admin", Permissions = new List<string> { "command-center:read", "command-center:write", "fleet-intelligence:read", "fleet-intelligence:write", "device-iot:read", "device-iot:write", "settings:read", "settings:write" }, IsSystemRole = true },
            new() { Id = driverRoleId, TenantId = tenant1Id, Name = "Driver", Permissions = new List<string> { "fleet-intelligence:read", "trip-logistics:read" }, IsSystemRole = true }
        };

        context.Roles.AddRange(roles);

        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Email = "admin@acme-logistics.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), FirstName = "Rajesh", LastName = "Kumar", RoleId = adminRoleId1 },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Email = "dispatcher@acme-logistics.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dispatch@123"), FirstName = "Priya", LastName = "Sharma", RoleId = adminRoleId1 },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Email = "admin@saferide-taxi.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), FirstName = "John", LastName = "Smith", RoleId = adminRoleId2 },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Email = "admin@gulf-mining.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), FirstName = "Ahmed", LastName = "Al-Rashid", RoleId = adminRoleId3 }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // ==========================================
        // VEHICLES (Sample fleet for each tenant)
        // ==========================================
        var vehicles1 = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VehicleNumber = "MH-01-AB-1234", Type = "Truck", Model = "Tata Prima 2525.K", Year = 2024, FuelType = "Diesel", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VehicleNumber = "MH-02-CD-5678", Type = "Truck", Model = "Ashok Leyland Viking", Year = 2023, FuelType = "Diesel", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VehicleNumber = "MH-03-EF-9012", Type = "Container", Model = "BharatBenz 2823C", Year = 2024, FuelType = "Diesel", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VehicleNumber = "KA-01-GH-3456", Type = "Tanker", Model = "Tata Signa 3118T", Year = 2023, FuelType = "Diesel", Status = "maintenance" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VehicleNumber = "DL-01-IJ-7890", Type = "Refrigerated", Model = "Eicher Pro 2110", Year = 2024, FuelType = "Diesel", Status = "active" }
        };

        var vehicles2 = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, VehicleNumber = "NYC-T-001", Type = "Sedan", Model = "Toyota Camry Hybrid", Year = 2024, FuelType = "Hybrid", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, VehicleNumber = "NYC-T-002", Type = "SUV", Model = "Ford Explorer", Year = 2023, FuelType = "Gasoline", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, VehicleNumber = "NYC-T-003", Type = "Sedan", Model = "Honda Accord", Year = 2024, FuelType = "Gasoline", Status = "active" }
        };

        var vehicles3 = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, VehicleNumber = "KSA-MINE-01", Type = "Heavy Equipment", Model = "CAT 785D Mining Truck", Year = 2022, FuelType = "Diesel", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, VehicleNumber = "KSA-MINE-02", Type = "Heavy Equipment", Model = "Komatsu HD785-7", Year = 2023, FuelType = "Diesel", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, VehicleNumber = "KSA-SUP-01", Type = "Support Vehicle", Model = "Toyota Land Cruiser", Year = 2024, FuelType = "Diesel", Status = "active" }
        };

        context.Vehicles.AddRange(vehicles1);
        context.Vehicles.AddRange(vehicles2);
        context.Vehicles.AddRange(vehicles3);
        await context.SaveChangesAsync();

        // ==========================================
        // DRIVERS
        // ==========================================
        var drivers = new List<Driver>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, FirstName = "Suresh", LastName = "Patil", LicenseNumber = "MH-DRV-001", LicenseExpiry = DateTime.UtcNow.AddYears(2), Phone = "+91-9876543210", BehaviorScore = 87.5m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, FirstName = "Vikram", LastName = "Singh", LicenseNumber = "MH-DRV-002", LicenseExpiry = DateTime.UtcNow.AddYears(1), Phone = "+91-9876543211", BehaviorScore = 92.3m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, FirstName = "Manoj", LastName = "Joshi", LicenseNumber = "KA-DRV-001", LicenseExpiry = DateTime.UtcNow.AddMonths(8), Phone = "+91-9876543212", BehaviorScore = 78.1m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, FirstName = "Arun", LastName = "Desai", LicenseNumber = "DL-DRV-001", LicenseExpiry = DateTime.UtcNow.AddYears(3), Phone = "+91-9876543213", BehaviorScore = 95.0m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, FirstName = "Michael", LastName = "Johnson", LicenseNumber = "NY-CDL-001", LicenseExpiry = DateTime.UtcNow.AddYears(2), Phone = "+1-555-0101", BehaviorScore = 88.7m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, FirstName = "Sarah", LastName = "Williams", LicenseNumber = "NY-CDL-002", LicenseExpiry = DateTime.UtcNow.AddYears(1), Phone = "+1-555-0102", BehaviorScore = 91.2m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, FirstName = "Mohammed", LastName = "Al-Farsi", LicenseNumber = "SA-DRV-001", LicenseExpiry = DateTime.UtcNow.AddYears(1), Phone = "+966-501234567", BehaviorScore = 85.6m, Status = "active" }
        };

        context.Drivers.AddRange(drivers);
        await context.SaveChangesAsync();

        // ==========================================
        // DEVICES (GPS units assigned to vehicles)
        // ==========================================
        var devices = new List<Device>
        {
            // Acme Logistics devices
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VendorId = vendor1Id, Imei = "867959033200001", SerialNumber = "IT-VT300-001", Model = "iTriangle VT300", VehicleId = vehicles1[0].Id, DriverId = drivers[0].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-2), LastSpeed = 65.5m, SignalStrength = -75, BatteryLevel = 87 },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VendorId = vendor1Id, Imei = "867959033200002", SerialNumber = "IT-VT300-002", Model = "iTriangle VT300", VehicleId = vehicles1[1].Id, DriverId = drivers[1].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-5), LastSpeed = 0m, SignalStrength = -82, BatteryLevel = 92 },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VendorId = vendor2Id, Imei = "863456012300001", SerialNumber = "ST-X1-001", Model = "Streamax X1", VehicleId = vehicles1[2].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-1), SignalStrength = -68, BatteryLevel = 100 },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VendorId = vendor3Id, Imei = "352093081200001", SerialNumber = "TL-FMC130-001", Model = "Teltonika FMC130", VehicleId = vehicles1[3].Id, DriverId = drivers[2].Id, Status = "offline", LastSeen = DateTime.UtcNow.AddHours(-2), SignalStrength = null, BatteryLevel = 45 },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VendorId = vendor1Id, Imei = "867959033200003", SerialNumber = "IT-VT300-003", Model = "iTriangle VT300", VehicleId = vehicles1[4].Id, DriverId = drivers[3].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-3), LastSpeed = 42.0m, SignalStrength = -71, BatteryLevel = 78 },

            // SafeRide Taxi devices
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, VendorId = vendor3Id, Imei = "352093081200002", SerialNumber = "TL-FMC600-001", Model = "Teltonika FMC600", VehicleId = vehicles2[0].Id, DriverId = drivers[4].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-1), LastSpeed = 35.0m, SignalStrength = -65, BatteryLevel = 95 },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, VendorId = vendor2Id, Imei = "863456012300002", SerialNumber = "ST-CM32-001", Model = "Streamax CM32", VehicleId = vehicles2[1].Id, DriverId = drivers[5].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-2), SignalStrength = -70, BatteryLevel = 100 },

            // Gulf Mining devices
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, VendorId = vendor3Id, Imei = "352093081200003", SerialNumber = "TL-FMC600-002", Model = "Teltonika FMC600", VehicleId = vehicles3[0].Id, DriverId = drivers[6].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-1), LastSpeed = 25.0m, SignalStrength = -78, BatteryLevel = 88 },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, VendorId = vendor1Id, Imei = "867959033200004", SerialNumber = "IT-VT300-004", Model = "iTriangle VT300", VehicleId = vehicles3[1].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-3), LastSpeed = 18.5m, SignalStrength = -80, BatteryLevel = 72 }
        };

        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // ==========================================
        // FEATURES (Enable sectors per tenant)
        // ==========================================
        var features = new List<Feature>
        {
            // Acme Logistics: Logistics + Fleet Intelligence + Safety + Analytics
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "fleet-intelligence", FeatureName = "vehicle-directory", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "fleet-intelligence", FeatureName = "driver-hub", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "fleet-intelligence", FeatureName = "fuel-analytics", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "fleet-intelligence", FeatureName = "maintenance-studio", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "trip-logistics", FeatureName = "trip-planner", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "trip-logistics", FeatureName = "active-deliveries", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "trip-logistics", FeatureName = "yard-dock", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "safety-compliance", FeatureName = "video-telematics", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "safety-compliance", FeatureName = "incident-center", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "analytics", FeatureName = "insight-builder", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Module = "device-iot", FeatureName = "device-fleet", Enabled = true },

            // SafeRide Taxi: Fleet Intelligence + Command Center
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Module = "fleet-intelligence", FeatureName = "vehicle-directory", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Module = "fleet-intelligence", FeatureName = "driver-hub", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Module = "command-center", FeatureName = "operations-overview", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Module = "command-center", FeatureName = "live-fleet-map", Enabled = true },

            // Gulf Mining: Fleet + Device + Safety
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Module = "fleet-intelligence", FeatureName = "vehicle-directory", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Module = "fleet-intelligence", FeatureName = "fuel-analytics", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Module = "device-iot", FeatureName = "device-fleet", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Module = "safety-compliance", FeatureName = "incident-center", Enabled = true }
        };

        context.Features.AddRange(features);

        // ==========================================
        // DEVICE COMMANDS (Recent command history)
        // ==========================================
        var commands = new List<DeviceCommand>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, DeviceId = devices[0].Id, CommandType = "poll", Status = "acknowledged", SentAt = DateTime.UtcNow.AddMinutes(-10), AcknowledgedAt = DateTime.UtcNow.AddMinutes(-9) },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, DeviceId = devices[3].Id, CommandType = "poll", Status = "failed", ErrorMessage = "Device offline", SentAt = DateTime.UtcNow.AddMinutes(-5) }
        };

        context.DeviceCommands.AddRange(commands);

        await context.SaveChangesAsync();

        // ==========================================
        // USER PREFERENCES (Default table configs)
        // ==========================================
        var userId1 = context.Users.First(u => u.TenantId == tenant1Id && u.Email == "admin@acme-logistics.com").Id;

        var preferences = new List<UserPreference>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId1,
                Page = "vehicle-directory",
                PreferenceType = "table-columns",
                Config = new Dictionary<string, object>
                {
                    ["columns"] = new List<Dictionary<string, object>>
                    {
                        new() { ["field"] = "vehicleNumber", ["header"] = "Vehicle #", ["visible"] = true, ["width"] = 150, ["order"] = 1 },
                        new() { ["field"] = "type", ["header"] = "Type", ["visible"] = true, ["width"] = 120, ["order"] = 2 },
                        new() { ["field"] = "model", ["header"] = "Model", ["visible"] = true, ["width"] = 200, ["order"] = 3 },
                        new() { ["field"] = "status", ["header"] = "Status", ["visible"] = true, ["width"] = 100, ["order"] = 4 },
                        new() { ["field"] = "fuelType", ["header"] = "Fuel", ["visible"] = true, ["width"] = 80, ["order"] = 5 },
                        new() { ["field"] = "year", ["header"] = "Year", ["visible"] = false, ["width"] = 80, ["order"] = 6 }
                    },
                    ["pageSize"] = 25,
                    ["defaultSort"] = new Dictionary<string, string> { ["field"] = "vehicleNumber", ["direction"] = "asc" }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId1,
                Page = "driver-hub",
                PreferenceType = "table-columns",
                Config = new Dictionary<string, object>
                {
                    ["columns"] = new List<Dictionary<string, object>>
                    {
                        new() { ["field"] = "name", ["header"] = "Driver Name", ["visible"] = true, ["width"] = 200, ["order"] = 1 },
                        new() { ["field"] = "licenseNumber", ["header"] = "License #", ["visible"] = true, ["width"] = 150, ["order"] = 2 },
                        new() { ["field"] = "behaviorScore", ["header"] = "Score", ["visible"] = true, ["width"] = 100, ["order"] = 3 },
                        new() { ["field"] = "phone", ["header"] = "Phone", ["visible"] = true, ["width"] = 140, ["order"] = 4 },
                        new() { ["field"] = "status", ["header"] = "Status", ["visible"] = true, ["width"] = 100, ["order"] = 5 }
                    }
                }
            }
        };

        context.UserPreferences.AddRange(preferences);
        await context.SaveChangesAsync();

        Console.WriteLine("[Seed] ✅ Sample data seeded successfully!");
        Console.WriteLine($"[Seed]   Tenant 1: {tenants[0].Name} (subdomain: {tenants[0].Subdomain})");
        Console.WriteLine($"[Seed]   Tenant 2: {tenants[1].Name} (subdomain: {tenants[1].Subdomain})");
        Console.WriteLine($"[Seed]   Tenant 3: {tenants[2].Name} (subdomain: {tenants[2].Subdomain})");
        Console.WriteLine($"[Seed]   Vendors:  {vendors.Count} device vendors registered");
        Console.WriteLine($"[Seed]   Vehicles: {vehicles1.Count + vehicles2.Count + vehicles3.Count} total");
        Console.WriteLine($"[Seed]   Drivers:  {drivers.Count} total");
        Console.WriteLine($"[Seed]   Devices:  {devices.Count} total");
        Console.WriteLine($"[Seed]   Features: {features.Count} module features enabled");
        Console.WriteLine();
        Console.WriteLine("[Seed] UAT Login Credentials:");
        Console.WriteLine($"[Seed]   Admin (Acme):    admin@acme-logistics.com / Admin@123");
        Console.WriteLine($"[Seed]   Admin (SafeRide): admin@saferide-taxi.com / Admin@123");
        Console.WriteLine($"[Seed]   Admin (Gulf):    admin@gulf-mining.com / Admin@123");
    }
}
