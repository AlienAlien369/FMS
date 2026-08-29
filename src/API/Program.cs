using FMS.Application;
using FMS.Infrastructure;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Override connection string from DATABASE_URL env var (Render/Neon provides this)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Neon/Render provides postgres:// or postgresql:// URLs
    // EF Core Npgsql needs the full connection string with sslmode
    if (!databaseUrl.Contains("sslmode"))
    {
        databaseUrl += databaseUrl.Contains("?") ? "&sslmode=require" : "?sslmode=require";
    }
    builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;
}

// JWT Secret from env var
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrEmpty(jwtSecret))
{
    builder.Configuration["Jwt:Key"] = jwtSecret;
    builder.Configuration["Jwt:Issuer"] = "FMS";
    builder.Configuration["Jwt:Audience"] = "FMS";
}

// CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("FmsCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201",
                "https://fms-web-uat.vercel.app",
                "https://fms-admin-uat.vercel.app",
                "https://fms-web-lakshyas-projects-c97e54f6.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FMS Fleet Management API", Version = "v1" });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

// Auto-migrate database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FmsDbContext>();
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Database migration completed");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Database migration failed: {ex.Message}");
    Console.WriteLine("   App will continue without database — health check will report unhealthy");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FmsCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Root endpoint for Render health check
app.MapGet("/", () => "FMS Fleet Management API is running 🚛");

Console.WriteLine($"🚀 FMS API starting on {string.Join(", ", builder.Configuration["Urls"] ?? "http://+:5000")}");
Console.WriteLine($"   Environment: {app.Environment.EnvironmentName}");

app.Run();
