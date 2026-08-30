using FMS.Domain.Entities;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Route = FMS.Domain.Entities.Route;

namespace FMS.API;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FmsDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if core data already exists
        if (await context.Tenants.AnyAsync())
        {
            // Core data exists — seed new productization tables if empty
            await SeedNewTables(context);
            return;
        }

        Console.WriteLine("[Seed] Seeding comprehensive data for UAT...");

        // ==========================================
        // TENANTS
        // ==========================================
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var tenant3Id = Guid.NewGuid();

        var tenants = new List<Tenant>
        {
            new()
            {
                Id = tenant1Id, Name = "Acme Logistics Corp", Subdomain = "acme-logistics",
                CountryCode = "IN", Timezone = "Asia/Kolkata", Currency = "INR",
                Plan = "pro", Status = "active", DataResidencyRegion = "ap-south-1",
                Settings = new Dictionary<string, object>
                {
                    ["branding"] = new Dictionary<string, string> { ["primaryColor"] = "#1e40af", ["secondaryColor"] = "#3b82f6" }
                }
            },
            new()
            {
                Id = tenant2Id, Name = "SafeRide Taxi Services", Subdomain = "saferide-taxi",
                CountryCode = "US", Timezone = "America/New_York", Currency = "USD",
                Plan = "basic", Status = "active", DataResidencyRegion = "us-east-1",
                Settings = new Dictionary<string, object>
                {
                    ["branding"] = new Dictionary<string, string> { ["primaryColor"] = "#059669", ["secondaryColor"] = "#10b981" }
                }
            },
            new()
            {
                Id = tenant3Id, Name = "Gulf Mining Group", Subdomain = "gulf-mining",
                CountryCode = "SA", Timezone = "Asia/Riyadh", Currency = "SAR",
                Plan = "enterprise", Status = "active", DataResidencyRegion = "me-south-1",
                Settings = new Dictionary<string, object>
                {
                    ["branding"] = new Dictionary<string, string> { ["primaryColor"] = "#dc2626", ["secondaryColor"] = "#ef4444" }
                }
            }
        };
        context.Tenants.AddRange(tenants);
        await context.SaveChangesAsync();

        // ==========================================
        // LOOKUPS (Dynamic dropdown values)
        // ==========================================
        var lookups = new List<Lookup>();

        // Countries
        var countryIds = new Dictionary<string, Guid>();
        foreach (var (code, label, phoneCode) in new[] { ("IN", "India", "+91"), ("US", "United States", "+1"), ("SA", "Saudi Arabia", "+966"), ("AE", "United Arab Emirates", "+971"), ("GB", "United Kingdom", "+44"), ("DE", "Germany", "+49"), ("JP", "Japan", "+81"), ("CN", "China", "+86"), ("BR", "Brazil", "+55"), ("AU", "Australia", "+61") })
        {
            var id = Guid.NewGuid();
            countryIds[code] = id;
            lookups.Add(new Lookup { Id = id, Category = "Country", Code = code, Label = label, SortOrder = lookups.Count, Metadata = new Dictionary<string, object> { ["phoneCode"] = phoneCode } });
        }

        // States (India)
        var stateIds = new Dictionary<string, Guid>();
        foreach (var (code, label, countryCode) in new[] { ("MH", "Maharashtra", "IN"), ("KA", "Karnataka", "IN"), ("DL", "Delhi", "IN"), ("TN", "Tamil Nadu", "IN"), ("GJ", "Gujarat", "IN"), ("NY", "New York", "US"), ("CA", "California", "US"), ("TX", "Texas", "US"), ("RI", "Riyadh", "SA"), ("MK", "Makkah", "SA") })
        {
            var id = Guid.NewGuid();
            stateIds[code] = id;
            lookups.Add(new Lookup { Id = id, Category = "State", ParentId = countryIds[countryCode], Code = code, Label = label, SortOrder = lookups.Count });
        }

        // Cities
        foreach (var (code, label, stateCode) in new[] { ("MUM", "Mumbai", "MH"), ("PUN", "Pune", "MH"), ("NGP", "Nagpur", "MH"), ("BLR", "Bangalore", "KA"), ("MYS", "Mysore", "KA"), ("DEL", "New Delhi", "DL"), ("CHN", "Chennai", "TN"), ("AHM", "Ahmedabad", "GJ"), ("NYC", "New York City", "NY"), ("LAX", "Los Angeles", "CA"), ("HOU", "Houston", "TX"), ("RUH", "Riyadh", "RI"), ("JED", "Jeddah", "MK") })
        {
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "City", ParentId = stateIds[stateCode], Code = code, Label = label, SortOrder = lookups.Count });
        }

        // Vehicle Types
        foreach (var (code, label) in new[] { ("TRUCK", "Truck"), ("VAN", "Van"), ("SEDAN", "Sedan"), ("SUV", "SUV"), ("TANKER", "Tanker"), ("CONTAINER", "Container"), ("REEFER", "Refrigerated"), ("HEAVY", "Heavy Equipment"), ("BIKE", "Bike"), ("BUS", "Bus"), ("SUPPORT", "Support Vehicle") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "VehicleType", Code = code, Label = label, SortOrder = lookups.Count });

        // Fuel Types
        foreach (var (code, label) in new[] { ("DIESEL", "Diesel"), ("PETROL", "Petrol"), ("CNG", "CNG"), ("LNG", "LNG"), ("ELECTRIC", "Electric"), ("HYBRID", "Hybrid"), ("GASOLINE", "Gasoline") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "FuelType", Code = code, Label = label, SortOrder = lookups.Count });

        // Route Types
        foreach (var (code, label) in new[] { ("FIXED", "Fixed"), ("DYNAMIC", "Dynamic"), ("ROUNDTRIP", "Round Trip"), ("MILKRUN", "Milk Run"), ("EXPRESS", "Express") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "RouteType", Code = code, Label = label, SortOrder = lookups.Count });

        // Location Types (for geofences)
        foreach (var (code, label) in new[] { ("PLANT", "Plant"), ("WAREHOUSE", "Warehouse"), ("CUSTOMER", "Customer Site"), ("YARD", "Yard"), ("PORT", "Port"), ("BORDER", "Border Checkpoint"), ("FUEL_STATION", "Fuel Station"), ("PARKING", "Parking Lot") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "LocationType", Code = code, Label = label, SortOrder = lookups.Count });

        // Geofence Colors
        foreach (var (code, label) in new[] { ("BLUE", "Blue"), ("RED", "Red"), ("GREEN", "Green"), ("YELLOW", "Yellow"), ("ORANGE", "Orange"), ("PURPLE", "Purple"), ("BLACK", "Black") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "GeofenceColor", Code = code, Label = label, SortOrder = lookups.Count });

        // Subscription Packages
        foreach (var (code, label) in new[] { ("BASIC", "Basic"), ("PRO", "Professional"), ("ENTERPRISE", "Enterprise"), ("CUSTOM", "Custom") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "SubscriptionPackage", Code = code, Label = label, SortOrder = lookups.Count });

        // Payment Modes
        foreach (var (code, label) in new[] { ("BANK", "Bank Transfer"), ("CARD", "Credit/Debit Card"), ("CASH", "Cash"), ("CHEQUE", "Cheque"), ("UPI", "UPI"), ("WALLET", "Digital Wallet") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "PaymentMode", Code = code, Label = label, SortOrder = lookups.Count });

        // Device Protocols
        foreach (var (code, label) in new[] { ("TCP", "TCP"), ("MQTT", "MQTT"), ("UDP", "UDP"), ("HTTP", "HTTP"), ("WIALON", "Wialon retarget") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "DeviceProtocol", Code = code, Label = label, SortOrder = lookups.Count });

        // Incident Severity
        foreach (var (code, label) in new[] { ("LOW", "Low"), ("MEDIUM", "Medium"), ("HIGH", "High"), ("CRITICAL", "Critical") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "IncidentSeverity", Code = code, Label = label, SortOrder = lookups.Count });

        // Incident Status
        foreach (var (code, label) in new[] { ("REPORTED", "Reported"), ("INVESTIGATING", "Investigating"), ("RESOLVED", "Resolved"), ("CLOSED", "Closed") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "IncidentStatus", Code = code, Label = label, SortOrder = lookups.Count });

        // Delivery Status
        foreach (var (code, label) in new[] { ("PENDING", "Pending"), ("PICKED_UP", "Picked Up"), ("IN_TRANSIT", "In Transit"), ("OUT_FOR_DELIVERY", "Out for Delivery"), ("DELIVERED", "Delivered"), ("FAILED", "Failed") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "DeliveryStatus", Code = code, Label = label, SortOrder = lookups.Count });

        // Consignee Category
        foreach (var (code, label) in new[] { ("REGULAR", "Regular"), ("VIP", "VIP"), ("GOVT", "Government"), ("EXPORT", "Export"), ("IMPORT", "Import") })
            lookups.Add(new Lookup { Id = Guid.NewGuid(), Category = "ConsigneeCategory", Code = code, Label = label, SortOrder = lookups.Count });

        context.Lookups.AddRange(lookups);
        await context.SaveChangesAsync();

        // ==========================================
        // DEVICE VENDORS
        // ==========================================
        var vendor1Id = Guid.NewGuid();
        var vendor2Id = Guid.NewGuid();
        var vendor3Id = Guid.NewGuid();

        context.DeviceVendors.AddRange(new List<DeviceVendor>
        {
            new() { Id = vendor1Id, Name = "iTriangle Infotech", Code = "itriangle", Protocol = "tcp", DefaultPort = 5001, SupportsFuel = true, SupportsTemperature = true },
            new() { Id = vendor2Id, Name = "Streamax Technology", Code = "streamax", Protocol = "mqtt", DefaultPort = 1883, SupportsVideo = true },
            new() { Id = vendor3Id, Name = "Teltonika Telematics", Code = "teltonika", Protocol = "tcp", DefaultPort = 5000, SupportsFuel = true, SupportsTemperature = true, SupportsCanBus = true }
        });
        await context.SaveChangesAsync();

        // ==========================================
        // USERS & ROLES
        // ==========================================
        var adminRoleId1 = Guid.NewGuid();
        var adminRoleId2 = Guid.NewGuid();
        var adminRoleId3 = Guid.NewGuid();
        var driverRoleId = Guid.NewGuid();

        var dispatcherRoleId1 = Guid.NewGuid();
        var managerRoleId1 = Guid.NewGuid();
        var viewerRoleId1 = Guid.NewGuid();
        context.Roles.AddRange(new List<Role>
        {
            new() { Id = adminRoleId1, TenantId = tenant1Id, Name = "Super Admin", Description = "Full system access", Permissions = new List<string> { "all" }, IsSystemRole = true },
            new() { Id = adminRoleId2, TenantId = tenant2Id, Name = "Super Admin", Description = "Full system access", Permissions = new List<string> { "all" }, IsSystemRole = true },
            new() { Id = adminRoleId3, TenantId = tenant3Id, Name = "Super Admin", Description = "Full system access", Permissions = new List<string> { "all" }, IsSystemRole = true },
            new() { Id = driverRoleId, TenantId = tenant1Id, Name = "Driver", Description = "View-only fleet and trip data", Permissions = new List<string> { "fleet-intelligence:read", "trip-logistics:read" }, IsSystemRole = true },
            new() { Id = dispatcherRoleId1, TenantId = tenant1Id, Name = "Dispatcher", Description = "Manage routes, vehicles, and daily operations", Permissions = new List<string> { "command-center:read", "fleet-intelligence:read", "trip-logistics:read", "trip-logistics:write" }, IsSystemRole = false, CreatedAt = DateTime.UtcNow },
            new() { Id = managerRoleId1, TenantId = tenant1Id, Name = "Manager", Description = "View all modules, manage drivers and clients", Permissions = new List<string> { "command-center:read", "fleet-intelligence:read", "trip-logistics:read", "safety-compliance:read", "analytics:read", "settings:read" }, IsSystemRole = false, CreatedAt = DateTime.UtcNow },
            new() { Id = viewerRoleId1, TenantId = tenant1Id, Name = "Viewer", Description = "Read-only access to dashboards", Permissions = new List<string> { "command-center:read", "analytics:read" }, IsSystemRole = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Name = "Fleet Manager", Description = "Manage vehicles, drivers, and devices", Permissions = new List<string> { "fleet-intelligence:all", "trip-logistics:read" }, IsSystemRole = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Name = "Safety Officer", Description = "Manage incidents, compliance, and safety", Permissions = new List<string> { "safety-compliance:all", "command-center:read" }, IsSystemRole = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Name = "Client Manager", Description = "Manage clients and their access", Permissions = new List<string> { "settings:clients", "command-center:read" }, IsSystemRole = false, CreatedAt = DateTime.UtcNow },
        });

        var userId1 = Guid.NewGuid();
        context.Users.AddRange(new List<User>
        {
            new() { Id = userId1, TenantId = tenant1Id, Email = "admin@acme-logistics.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), FirstName = "Rajesh", LastName = "Kumar", RoleId = adminRoleId1 },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Email = "dispatcher@acme-logistics.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dispatch@123"), FirstName = "Priya", LastName = "Sharma", RoleId = adminRoleId1 },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Email = "admin@saferide-taxi.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), FirstName = "John", LastName = "Smith", RoleId = adminRoleId2 },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Email = "admin@gulf-mining.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), FirstName = "Ahmed", LastName = "Al-Rashid", RoleId = adminRoleId3 }
        });
        await context.SaveChangesAsync();

        // ==========================================
        // VEHICLES
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
        context.Drivers.AddRange(new List<Driver>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, FirstName = "Suresh", LastName = "Patil", LicenseNumber = "MH-DRV-001", LicenseExpiry = DateTime.UtcNow.AddYears(2), Phone = "+91-9876543210", BehaviorScore = 87.5m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, FirstName = "Vikram", LastName = "Singh", LicenseNumber = "MH-DRV-002", LicenseExpiry = DateTime.UtcNow.AddYears(1), Phone = "+91-9876543211", BehaviorScore = 92.3m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, FirstName = "Michael", LastName = "Johnson", LicenseNumber = "NY-CDL-001", LicenseExpiry = DateTime.UtcNow.AddYears(2), Phone = "+1-555-0101", BehaviorScore = 88.7m, Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, FirstName = "Mohammed", LastName = "Al-Farsi", LicenseNumber = "SA-DRV-001", LicenseExpiry = DateTime.UtcNow.AddYears(1), Phone = "+966-501234567", BehaviorScore = 85.6m, Status = "active" }
        });
        await context.SaveChangesAsync();

        // ==========================================
        // FORM MASTERS (System pages for RBAC)
        // ==========================================
        var formMasters = new List<FormMaster>();
        var formNames = new[]
        {
            ("Dashboard", "HomeController", "Index", "Web"),
            ("AI Safety Dashboard", "SafetyController", "AiDashboard", "Web"),
            ("Analytics Dashboard", "AnalyticsController", "Dashboard", "Web"),
            ("Area Performance", "AnalyticsController", "AreaPerformance", "Web"),
            ("ATMS Customer Dashboard", "AtmsController", "CustomerDashboard", "Web"),
            ("ATMS Trip Dashboard", "AtmsController", "TripDashboard", "Web"),
            ("Attendance Dashboard", "AttendanceController", "Dashboard", "Web"),
            ("CCTV Video Wall", "VideoController", "Wall", "Web"),
            ("Cloud Video Playback", "VideoController", "Playback", "Web"),
            ("Command Center", "CommandCenter", "Index", "Web"),
            ("Compliance Dashboard", "ComplianceController", "Dashboard", "Web"),
            ("CTMS Trip Dashboard", "CtmsController", "TripDashboard", "Web"),
            ("Customer Dashboard", "CustomerController", "Dashboard", "Web"),
            ("Dashboard Data Detail", "DashboardController", "DataDetail", "Web"),
            ("Detail Dashboard", "DashboardController", "Detail", "Web"),
            ("Fleet Management System", "FleetController", "Index", "Web"),
            ("Incident Center", "SafetyController", "Incidents", "Web"),
            ("Live Fleet Map", "MapController", "Live", "Web"),
            ("Maintenance Studio", "MaintenanceController", "Index", "Web"),
            ("Notification Center", "NotificationController", "Index", "Web"),
            ("Operations Overview", "CommandCenter", "Overview", "Web"),
            ("OBD Dashboard", "ObdController", "Dashboard", "Web"),
            ("Reports", "ReportsController", "Index", "Web"),
            ("Route Management", "RouteController", "Index", "Web"),
            ("Geofence Management", "GeofenceController", "Index", "Web"),
            ("Settings", "SettingsController", "Index", "Web"),
            ("Track on Map", "MapController", "Track", "Web"),
            ("Transport Management System", "TransportController", "Index", "Web"),
            ("User Management", "UserController", "Index", "Web"),
            ("Vehicle Directory", "FleetController", "Vehicles", "Web"),
            ("Driver Hub", "FleetController", "Drivers", "Web"),
            ("Device Fleet", "DeviceController", "Index", "Web"),
            ("Client Management", "ClientController", "Index", "Web"),
            ("Subscription Management", "SubscriptionController", "Index", "Web"),
            ("Role Permissions", "RbacController", "RolePermissions", "Web"),
            ("Form Company Mapping", "RbacController", "CompanyMapping", "Web"),
            ("Form Column Config", "RbacController", "ColumnConfig", "Web"),
            ("Lookup Management", "LookupController", "Index", "Web"),
            ("Fuel Analytics", "FleetController", "FuelAnalytics", "Web"),
            ("Trip Planner", "TripController", "Planner", "Web"),
            ("Active Deliveries", "TripController", "Deliveries", "Web"),
            ("Yard & Dock", "YardController", "Index", "Web"),
            ("Video Telematics", "SafetyController", "Video", "Web"),
            ("Insight Builder", "AnalyticsController", "Insights", "Web")
        };

        foreach (var (name, controller, action, platform) in formNames)
        {
            var id = Guid.NewGuid();
            formMasters.Add(new FormMaster { Id = id, FormName = name, ControllerName = controller, ActionName = action, Platform = platform, IsActive = true, CreatedAt = DateTime.UtcNow });
        }
        context.FormMasters.AddRange(formMasters);
        await context.SaveChangesAsync();

        // ==========================================
        // FORM ROLE MAPPING (All forms → Super Admin for all tenants)
        // ==========================================
        foreach (var roleId in new[] { adminRoleId1, adminRoleId2, adminRoleId3 })
        {
            var tenantId = roleId == adminRoleId1 ? tenant1Id : roleId == adminRoleId2 ? tenant2Id : tenant3Id;
            foreach (var form in formMasters)
            {
                context.FormRoleMappings.Add(new FormRoleMapping
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, RoleId = roleId, FormId = form.Id,
                    CanView = true, CanAdd = true, CanEdit = true, CanDelete = true, CreatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();

        // ==========================================
        // FORM ROLE MAPPING for Dispatcher, Manager, Viewer
        // ==========================================
        var dispatcherForms = formMasters.Where(f => new[] { "Operations Overview", "Live Fleet Map", "Vehicle Directory", "Driver Hub", "Route Management", "Geofence Management", "Active Deliveries", "Command Center" }.Contains(f.FormName)).ToList();
        foreach (var form in dispatcherForms)
        {
            context.FormRoleMappings.Add(new FormRoleMapping
            {
                Id = Guid.NewGuid(), TenantId = tenant1Id, RoleId = dispatcherRoleId1, FormId = form.Id,
                CanView = true, CanAdd = true, CanEdit = true, CanDelete = false, CreatedAt = DateTime.UtcNow
            });
        }
        var managerForms = formMasters.Where(f => new[] { "Operations Overview", "Live Fleet Map", "Vehicle Directory", "Driver Hub", "Fuel Analytics", "Maintenance Studio", "Route Management", "Geofence Management", "Active Deliveries", "Incident Center", "Insight Builder", "Command Center", "Client Management", "Device Fleet" }.Contains(f.FormName)).ToList();
        foreach (var form in managerForms)
        {
            context.FormRoleMappings.Add(new FormRoleMapping
            {
                Id = Guid.NewGuid(), TenantId = tenant1Id, RoleId = managerRoleId1, FormId = form.Id,
                CanView = true, CanAdd = false, CanEdit = true, CanDelete = false, CreatedAt = DateTime.UtcNow
            });
        }
        var viewerForms = formMasters.Where(f => new[] { "Operations Overview", "Live Fleet Map", "Vehicle Directory", "Insight Builder", "Command Center" }.Contains(f.FormName)).ToList();
        foreach (var form in viewerForms)
        {
            context.FormRoleMappings.Add(new FormRoleMapping
            {
                Id = Guid.NewGuid(), TenantId = tenant1Id, RoleId = viewerRoleId1, FormId = form.Id,
                CanView = true, CanAdd = false, CanEdit = false, CanDelete = false, CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        // ==========================================
        // FORM COMPANY MAPPING (Enable all forms for all tenants)
        // ==========================================
        foreach (var tid in new[] { tenant1Id, tenant2Id, tenant3Id })
        {
            foreach (var form in formMasters)
            {
                context.FormCompanyMappings.Add(new FormCompanyMapping
                {
                    Id = Guid.NewGuid(), TenantId = tid, FormId = form.Id,
                    IsEnabled = true, CreatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();

        // ==========================================
        // FEATURES
        // ==========================================
        context.Features.AddRange(new List<Feature>
        {
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
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Module = "fleet-intelligence", FeatureName = "vehicle-directory", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Module = "fleet-intelligence", FeatureName = "driver-hub", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, Module = "command-center", FeatureName = "operations-overview", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Module = "fleet-intelligence", FeatureName = "vehicle-directory", Enabled = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, Module = "device-iot", FeatureName = "device-fleet", Enabled = true }
        });

        // ==========================================
        // CLIENTS (Sample data for Acme)
        // ==========================================
        context.Clients.AddRange(new List<Client>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, ClientName = "Reliance Industries", ClientCode = "REL-001", CompanyName = "Reliance Industries Ltd", Address = "Maker Chambers IV, Nariman Point", CountryId = countryIds["IN"], StateId = stateIds["MH"], CityId = lookups.First(l => l.Code == "MUM").Id, ContactPerson = "Amit Patel", ContactNo = "+91-22-22785000", MobileNo = "+91-9820012345", EmailId = "amit.patel@ril.com", GstNo = "27AABCR1234M1Z5", IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, ClientName = "Tata Motors", ClientCode = "TML-001", CompanyName = "Tata Motors Ltd", Address = "Bombay House, Homi Mody Street", CountryId = countryIds["IN"], StateId = stateIds["MH"], CityId = lookups.First(l => l.Code == "MUM").Id, ContactPerson = "Sanjay Mehta", ContactNo = "+91-22-66657000", MobileNo = "+91-9820098765", EmailId = "sanjay@tatamotors.com", IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, ClientName = "Amazon India", ClientCode = "AMZ-001", CompanyName = "Amazon Seller Services Pvt Ltd", Address = "Embassy Tech Village, Outer Ring Road", CountryId = countryIds["IN"], StateId = stateIds["KA"], CityId = lookups.First(l => l.Code == "BLR").Id, ContactPerson = "Deepak Nair", ContactNo = "+91-80-41970000", MobileNo = "+91-9900112233", EmailId = "deepak@amazon.in", IsActive = true }
        });

        // ==========================================
        // ROUTES (Sample for Acme)
        // ==========================================
        context.Routes.AddRange(new List<Route>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, RouteName = "Mumbai-Pune Express", StartLocation = "Mumbai, Maharashtra", EndLocation = "Pune, Maharashtra", StartLatitude = 19.0760m, StartLongitude = 72.8777m, EndLatitude = 18.5204m, EndLongitude = 73.8567m, DistanceKm = 149.0m, EstimatedDurationMin = 180, IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, RouteName = "Delhi-Jaipur Highway", StartLocation = "New Delhi", EndLocation = "Jaipur, Rajasthan", StartLatitude = 28.6139m, StartLongitude = 77.2090m, EndLatitude = 26.9124m, EndLongitude = 75.7873m, DistanceKm = 281.0m, EstimatedDurationMin = 300, IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, RouteName = "Bangalore-Chennai Corridor", StartLocation = "Bangalore, Karnataka", EndLocation = "Chennai, Tamil Nadu", StartLatitude = 12.9716m, StartLongitude = 77.5946m, EndLatitude = 13.0827m, EndLongitude = 80.2707m, DistanceKm = 346.0m, EstimatedDurationMin = 390, IsActive = true }
        });

        // ==========================================
        // GEOFENCES (Sample for Acme)
        // ==========================================
        context.Geofences.AddRange(new List<Geofence>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Name = "Mumbai Warehouse", Address = "Nhava Sheva, Navi Mumbai", Latitude = 18.9498m, Longitude = 72.9427m, RadiusMeters = 500m, Color = "Blue", IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Name = "Pune Distribution Center", Address = "Pimpri-Chinchwad, Pune", Latitude = 18.6492m, Longitude = 73.8314m, RadiusMeters = 750m, Color = "Green", IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, Name = "Delhi Border Checkpoint", Address = "Delhi-Gurgaon Border", Latitude = 28.4595m, Longitude = 77.0266m, RadiusMeters = 300m, Color = "Red", IsActive = true }
        });

        // ==========================================
        // SUBSCRIPTIONS (Sample)
        // ==========================================
        context.Subscriptions.AddRange(new List<Subscription>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, PackageName = "Professional", SubscriptionFrom = new DateOnly(2024, 1, 1), SubscriptionTo = new DateOnly(2025, 12, 31), InvoiceNo = "INV-2024-001", InvoiceDate = new DateOnly(2024, 1, 5), IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, PackageName = "Basic", SubscriptionFrom = new DateOnly(2024, 6, 1), SubscriptionTo = new DateOnly(2025, 5, 31), InvoiceNo = "INV-2024-042", InvoiceDate = new DateOnly(2024, 6, 10), IsActive = true },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, PackageName = "Enterprise", SubscriptionFrom = new DateOnly(2024, 3, 1), SubscriptionTo = new DateOnly(2027, 2, 28), InvoiceNo = "INV-2024-089", InvoiceDate = new DateOnly(2024, 3, 15), IsActive = true }
        });

        // ==========================================
        // DEVICES
        // ==========================================
        context.Devices.AddRange(new List<Device>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VendorId = vendor1Id, Imei = "867959033200001", SerialNumber = "IT-VT300-001", Model = "iTriangle VT300", VehicleId = vehicles1[0].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-2), LastSpeed = 65.5m, SignalStrength = -75, BatteryLevel = 87 },
            new() { Id = Guid.NewGuid(), TenantId = tenant1Id, VendorId = vendor1Id, Imei = "867959033200002", SerialNumber = "IT-VT300-002", Model = "iTriangle VT300", VehicleId = vehicles1[1].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-5), LastSpeed = 0m, SignalStrength = -82, BatteryLevel = 92 },
            new() { Id = Guid.NewGuid(), TenantId = tenant2Id, VendorId = vendor3Id, Imei = "352093081200002", SerialNumber = "TL-FMC600-001", Model = "Teltonika FMC600", VehicleId = vehicles2[0].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-1), LastSpeed = 35.0m, SignalStrength = -65, BatteryLevel = 95 },
            new() { Id = Guid.NewGuid(), TenantId = tenant3Id, VendorId = vendor3Id, Imei = "352093081200003", SerialNumber = "TL-FMC600-002", Model = "Teltonika FMC600", VehicleId = vehicles3[0].Id, Status = "active", LastSeen = DateTime.UtcNow.AddMinutes(-1), LastSpeed = 25.0m, SignalStrength = -78, BatteryLevel = 88 }
        });

        await context.SaveChangesAsync();

        Console.WriteLine("[Seed] ✅ Comprehensive data seeded successfully!");
        Console.WriteLine($"[Seed]   Tenants: {tenants.Count}");
        Console.WriteLine($"[Seed]   Lookups: {lookups.Count} values across {lookups.Select(l => l.Category).Distinct().Count()} categories");
        Console.WriteLine($"[Seed]   Forms: {formMasters.Count} system pages registered");
        Console.WriteLine($"[Seed]   Vehicles: {vehicles1.Count + vehicles2.Count + vehicles3.Count}");
        Console.WriteLine("[Seed] Login: admin@acme-logistics.com / Admin@123");
        Console.WriteLine("[Seed] Login: admin@saferide-taxi.com / Admin@123");
        Console.WriteLine("[Seed] Login: admin@gulf-mining.com / Admin@123");
    }

    private static async Task SeedNewTables(FmsDbContext context)
    {
        // Seed Lookups if empty
        if (!await context.Lookups.AnyAsync())
        {
            Console.WriteLine("[Seed] Seeding Lookups...");
            var lookups = new List<Lookup>();
            int sort = 0;
            // Country
            foreach (var (code, label) in new[] { ("IN", "India"), ("US", "United States"), ("SA", "Saudi Arabia"), ("AE", "UAE"), ("GB", "United Kingdom"), ("DE", "Germany"), ("JP", "Japan"), ("AU", "Australia"), ("CA", "Canada"), ("BR", "Brazil") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "Country", Code = code, Label = label, SortOrder = sort++ });
            // VehicleType
            sort = 0;
            foreach (var (code, label) in new[] { ("TRUCK", "Truck"), ("VAN", "Van"), ("TAXI", "Taxi"), ("BUS", "Bus"), ("BIKE", "Bike"), ("TRAILER", "Trailer"), ("CONTAINER", "Container"), ("TANKER", "Tanker"), ("REFRIGERATED", "Refrigerated"), ("FLATBED", "Flatbed"), ("MINI", "Mini Truck") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "VehicleType", Code = code, Label = label, SortOrder = sort++ });
            // FuelType
            sort = 0;
            foreach (var (code, label) in new[] { ("DIESEL", "Diesel"), ("PETROL", "Petrol"), ("CNG", "CNG"), ("LNG", "LNG"), ("ELECTRIC", "Electric"), ("HYBRID", "Hybrid"), ("HSD", "HSD") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "FuelType", Code = code, Label = label, SortOrder = sort++ });
            // RouteType
            sort = 0;
            foreach (var (code, label) in new[] { ("SHUTTLE", "Shuttle"), ("LONG_DISTANCE", "Long Distance"), ("LOCAL", "Local"), ("EXPRESS", "Express"), ("RETURN", "Return") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "RouteType", Code = code, Label = label, SortOrder = sort++ });
            // LocationType
            sort = 0;
            foreach (var (code, label) in new[] { ("WAREHOUSE", "Warehouse"), ("OFFICE", "Office"), ("YARD", "Yard"), ("DOCK", "Dock"), ("PARKING", "Parking"), ("FUEL_STATION", "Fuel Station"), ("CUSTOMER", "Customer"), ("SUPPLIER", "Supplier") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "LocationType", Code = code, Label = label, SortOrder = sort++ });
            // GeofenceColor
            sort = 0;
            foreach (var (code, label) in new[] { ("BLUE", "Blue"), ("RED", "Red"), ("GREEN", "Green"), ("YELLOW", "Yellow"), ("ORANGE", "Orange"), ("PURPLE", "Purple"), ("CYAN", "Cyan") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "GeofenceColor", Code = code, Label = label, SortOrder = sort++ });
            // SubscriptionPackage
            sort = 0;
            foreach (var (code, label) in new[] { ("BASIC", "Basic"), ("PROFESSIONAL", "Professional"), ("ENTERPRISE", "Enterprise"), ("CUSTOM", "Custom") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "SubscriptionPackage", Code = code, Label = label, SortOrder = sort++ });
            // PaymentMode
            sort = 0;
            foreach (var (code, label) in new[] { ("BANK_TRANSFER", "Bank Transfer"), ("CREDIT_CARD", "Credit Card"), ("DEBIT_CARD", "Debit Card"), ("CASH", "Cash"), ("CHEQUE", "Cheque"), ("UPI", "UPI") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "PaymentMode", Code = code, Label = label, SortOrder = sort++ });
            // IncidentSeverity
            sort = 0;
            foreach (var (code, label) in new[] { ("LOW", "Low"), ("MEDIUM", "Medium"), ("HIGH", "High"), ("CRITICAL", "Critical") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "IncidentSeverity", Code = code, Label = label, SortOrder = sort++ });
            // DeliveryStatus
            sort = 0;
            foreach (var (code, label) in new[] { ("PENDING", "Pending"), ("PICKED_UP", "Picked Up"), ("IN_TRANSIT", "In Transit"), ("OUT_FOR_DELIVERY", "Out for Delivery"), ("DELIVERED", "Delivered"), ("FAILED", "Failed") })
                lookups.Add(new() { Id = Guid.NewGuid(), Category = "DeliveryStatus", Code = code, Label = label, SortOrder = sort++ });
            context.Lookups.AddRange(lookups);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seed]   Lookups: {lookups.Count} values");
        }

        // Seed FormMasters if empty
        if (!await context.FormMasters.AnyAsync())
        {
            Console.WriteLine("[Seed] Seeding FormMasters...");
            var forms = new List<FormMaster>();
            var formDefs = new[] {
                ("Operations Overview", "OperationsController", "Overview", "Web"),
                ("Live Fleet Map", "FleetController", "LiveMap", "Web"),
                ("Vehicle Directory", "VehiclesController", "Index", "Web"),
                ("Driver Hub", "DriversController", "Index", "Web"),
                ("Fuel Analytics", "AnalyticsController", "Fuel", "Web"),
                ("Maintenance Studio", "MaintenanceController", "Index", "Web"),
                ("Route Management", "RoutesController", "Index", "Web"),
                ("Geofence Management", "GeofencesController", "Index", "Web"),
                ("Active Deliveries", "DeliveriesController", "Index", "Web"),
                ("Video Telematics", "TelematicsController", "Index", "Web"),
                ("Incident Center", "IncidentsController", "Index", "Web"),
                ("Device Fleet", "DevicesController", "Index", "Web"),
                ("Insight Builder", "AnalyticsController", "Insights", "Web"),
                ("Company Configuration", "SettingsController", "Company", "Web"),
                ("User Management", "UsersController", "Index", "Web"),
                ("Client Management", "ClientsController", "Index", "Web"),
                ("Role & Permissions", "RbacController", "Index", "Web"),
                ("Lookup Management", "LookupsController", "Index", "Web"),
                ("Form Registry", "FormMastersController", "Index", "Web"),
                ("Feature Management", "FeaturesController", "Index", "Web"),
                ("Subscription", "SubscriptionsController", "Index", "Web"),
                ("Notifications", "NotificationsController", "Index", "Web"),
                ("Audit Trail", "AuditController", "Index", "Web"),
            };
            foreach (var (fn, cn, an, pl) in formDefs)
                forms.Add(new() { Id = Guid.NewGuid(), FormName = fn, ControllerName = cn, ActionName = an, Platform = pl, IsActive = true });
            context.FormMasters.AddRange(forms);
            await context.SaveChangesAsync();
            Console.WriteLine($"[Seed]   FormMasters: {forms.Count} forms registered");
        }

        // Seed sample clients if empty
        var tenantIds = await context.Tenants.Select(t => t.Id).ToListAsync();
        if (tenantIds.Any() && !await context.Clients.AnyAsync())
        {
            Console.WriteLine("[Seed] Seeding Clients...");
            var tid = tenantIds.First();
            context.Clients.AddRange(new List<Client>
            {
                new() { Id = Guid.NewGuid(), TenantId = tid, ClientName = "Reliance Industries", ClientCode = "REL-001", CompanyName = "Reliance Industries Ltd", Address = "Maker Chambers IV, Nariman Point", ContactPerson = "Amit Patel", ContactNo = "+91-22-22785000", MobileNo = "+91-9820012345", EmailId = "amit@ril.com", IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tid, ClientName = "Tata Steel", ClientCode = "TATA-001", CompanyName = "Tata Steel Ltd", Address = "Bandra Kurla Complex", ContactPerson = "Priya Sharma", ContactNo = "+91-22-66652000", MobileNo = "+91-9820067890", EmailId = "priya@tatasteel.com", IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tid, ClientName = "L&T Infrastructure", ClientCode = "LT-001", CompanyName = "Larsen & Toubro Ltd", Address = "Nations Height, Bandra East", ContactPerson = "Rahul Mehta", ContactNo = "+91-22-67525000", MobileNo = "+91-9820011122", EmailId = "rahul@lt.com", IsActive = true },
            });
            await context.SaveChangesAsync();
            Console.WriteLine("[Seed]   Clients: 3 sample clients");
        }

        // Seed sample routes if empty
        if (tenantIds.Any() && !await context.Routes.AnyAsync())
        {
            Console.WriteLine("[Seed] Seeding Routes...");
            var tid = tenantIds.First();
            context.Routes.AddRange(new List<Route>
            {
                new() { Id = Guid.NewGuid(), TenantId = tid, RouteName = "Mumbai-Pune Express", StartLocation = "Mumbai", EndLocation = "Pune", StartLatitude = 19.076m, StartLongitude = 72.8777m, EndLatitude = 18.5204m, EndLongitude = 73.8567m, DistanceKm = 149, EstimatedDurationMin = 180, IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tid, RouteName = "Mumbai-Delhi Highway", StartLocation = "Mumbai", EndLocation = "Delhi", StartLatitude = 19.076m, StartLongitude = 72.8777m, EndLatitude = 28.6139m, EndLongitude = 77.209m, DistanceKm = 1400, EstimatedDurationMin = 1800, IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tid, RouteName = "Pune-Bangalore Route", StartLocation = "Pune", EndLocation = "Bangalore", StartLatitude = 18.5204m, StartLongitude = 73.8567m, EndLatitude = 12.9716m, EndLongitude = 77.5946m, DistanceKm = 840, EstimatedDurationMin = 960, IsActive = true },
            });
            await context.SaveChangesAsync();
            Console.WriteLine("[Seed]   Routes: 3 sample routes");
        }

        // Seed sample geofences if empty
        if (tenantIds.Any() && !await context.Geofences.AnyAsync())
        {
            Console.WriteLine("[Seed] Seeding Geofences...");
            var tid = tenantIds.First();
            context.Geofences.AddRange(new List<Geofence>
            {
                new() { Id = Guid.NewGuid(), TenantId = tid, Name = "Mumbai Warehouse", Address = "JNPT Area", Latitude = 18.9494m, Longitude = 72.9388m, RadiusMeters = 500, Color = "Blue", IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tid, Name = "Pune Depot", Address = "Chakan Industrial Area", Latitude = 18.7606m, Longitude = 73.8593m, RadiusMeters = 300, Color = "Green", IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tid, Name = "Delhi Hub", Address = "Bawana Industrial Area", Latitude = 28.7966m, Longitude = 77.0567m, RadiusMeters = 400, Color = "Red", IsActive = true },
            });
            await context.SaveChangesAsync();
            Console.WriteLine("[Seed]   Geofences: 3 sample geofences");
        }

        // Seed sample subscriptions if empty
        if (tenantIds.Any() && !await context.Subscriptions.AnyAsync())
        {
            Console.WriteLine("[Seed] Seeding Subscriptions...");
            context.Subscriptions.AddRange(new List<Subscription>
            {
                new() { Id = Guid.NewGuid(), TenantId = tenantIds[0], PackageName = "Professional", SubscriptionFrom = new DateOnly(2025, 1, 1), SubscriptionTo = new DateOnly(2026, 12, 31), InvoiceNo = "INV-2025-001", InvoiceDate = new DateOnly(2025, 1, 15), IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tenantIds.Count > 1 ? tenantIds[1] : tenantIds[0], PackageName = "Basic", SubscriptionFrom = new DateOnly(2025, 3, 1), SubscriptionTo = new DateOnly(2026, 2, 28), InvoiceNo = "INV-2025-042", InvoiceDate = new DateOnly(2025, 3, 10), IsActive = true },
                new() { Id = Guid.NewGuid(), TenantId = tenantIds.Count > 2 ? tenantIds[2] : tenantIds[0], PackageName = "Enterprise", SubscriptionFrom = new DateOnly(2024, 6, 1), SubscriptionTo = new DateOnly(2027, 5, 31), InvoiceNo = "INV-2024-089", InvoiceDate = new DateOnly(2024, 6, 15), IsActive = true },
            });
            await context.SaveChangesAsync();
            Console.WriteLine("[Seed]   Subscriptions: 3 subscriptions");
        }

        // Seed additional roles if only Super Admin exists for first tenant
        if (tenantIds.Any())
        {
            var tid = tenantIds.First();
            var existingRoleNames = await context.Roles.Where(r => r.TenantId == tid).Select(r => r.Name).ToListAsync();
            var rolesToAdd = new List<Role>();
            var rolePerms = new (string Name, string Desc, List<string> Perms, bool IsSystem)[]
            {
                ("Dispatcher", "Manage routes, vehicles, and daily operations", new List<string> { "command-center:read", "fleet-intelligence:read", "trip-logistics:read", "trip-logistics:write" }, false),
                ("Manager", "View all modules, manage drivers and clients", new List<string> { "command-center:read", "fleet-intelligence:read", "trip-logistics:read", "safety-compliance:read", "analytics:read", "settings:read" }, false),
                ("Viewer", "Read-only access to dashboards", new List<string> { "command-center:read", "analytics:read" }, false),
                ("Fleet Manager", "Manage vehicles, drivers, and devices", new List<string> { "fleet-intelligence:all", "trip-logistics:read" }, false),
                ("Safety Officer", "Manage incidents, compliance, and safety", new List<string> { "safety-compliance:all", "command-center:read" }, false),
                ("Client Manager", "Manage clients and their access", new List<string> { "settings:clients", "command-center:read" }, false),
            };
            foreach (var (name, desc, perms, isSys) in rolePerms)
            {
                if (!existingRoleNames.Contains(name))
                {
                    rolesToAdd.Add(new Role { Id = Guid.NewGuid(), TenantId = tid, Name = name, Description = desc, Permissions = perms, IsSystemRole = isSys, CreatedAt = DateTime.UtcNow });
                }
            }
            if (rolesToAdd.Any())
            {
                context.Roles.AddRange(rolesToAdd);
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seed]   Roles: {rolesToAdd.Count} new roles added");

                // Seed form-role mappings for new roles
                var forms = await context.FormMasters.ToListAsync();
                var allRoles = await context.Roles.Where(r => r.TenantId == tid).ToListAsync();
                var dispatcherRole = allRoles.FirstOrDefault(r => r.Name == "Dispatcher");
                var managerRole = allRoles.FirstOrDefault(r => r.Name == "Manager");
                var viewerRole = allRoles.FirstOrDefault(r => r.Name == "Viewer");
                var existingMappings = await context.FormRoleMappings.Where(m => m.TenantId == tid).ToListAsync();
                var existingPairs = new HashSet<string>(existingMappings.Select(m => m.RoleId.ToString() + "_" + m.FormId.ToString()));
                var newMappings = new List<FormRoleMapping>();
                if (dispatcherRole != null)
                {
                    var dispForms = forms.Where(f => new[] { "Operations Overview", "Live Fleet Map", "Vehicle Directory", "Driver Hub", "Route Management", "Geofence Management", "Active Deliveries", "Command Center" }.Contains(f.FormName)).ToList();
                    foreach (var f in dispForms)
                    {
                        if (!existingPairs.Contains(dispatcherRole.Id.ToString() + "_" + f.Id.ToString()))
                            newMappings.Add(new FormRoleMapping { Id = Guid.NewGuid(), TenantId = tid, RoleId = dispatcherRole.Id, FormId = f.Id, CanView = true, CanAdd = true, CanEdit = true, CanDelete = false, CreatedAt = DateTime.UtcNow });
                    }
                }
                if (managerRole != null)
                {
                    var mgrForms = forms.Where(f => new[] { "Operations Overview", "Live Fleet Map", "Vehicle Directory", "Driver Hub", "Fuel Analytics", "Maintenance Studio", "Route Management", "Geofence Management", "Active Deliveries", "Incident Center", "Insight Builder", "Command Center", "Client Management", "Device Fleet" }.Contains(f.FormName)).ToList();
                    foreach (var f in mgrForms)
                    {
                        if (!existingPairs.Contains(managerRole.Id.ToString() + "_" + f.Id.ToString()))
                            newMappings.Add(new FormRoleMapping { Id = Guid.NewGuid(), TenantId = tid, RoleId = managerRole.Id, FormId = f.Id, CanView = true, CanAdd = false, CanEdit = true, CanDelete = false, CreatedAt = DateTime.UtcNow });
                    }
                }
                if (viewerRole != null)
                {
                    var viewForms = forms.Where(f => new[] { "Operations Overview", "Live Fleet Map", "Vehicle Directory", "Insight Builder", "Command Center" }.Contains(f.FormName)).ToList();
                    foreach (var f in viewForms)
                    {
                        if (!existingPairs.Contains(viewerRole.Id.ToString() + "_" + f.Id.ToString()))
                            newMappings.Add(new FormRoleMapping { Id = Guid.NewGuid(), TenantId = tid, RoleId = viewerRole.Id, FormId = f.Id, CanView = true, CanAdd = false, CanEdit = false, CanDelete = false, CreatedAt = DateTime.UtcNow });
                    }
                }
                if (newMappings.Any())
                {
                    context.FormRoleMappings.AddRange(newMappings);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[Seed]   FormRoleMappings: {newMappings.Count} mappings added");
                }
            }
        }

        Console.WriteLine("[Seed] ✅ New tables seeded");
    }
}
