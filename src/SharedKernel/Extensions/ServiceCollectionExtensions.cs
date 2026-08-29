using System.Text;
using FMS.SharedKernel.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace FMS.SharedKernel.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all SharedKernel services: Serilog, OpenTelemetry, HealthChecks, JWT, OpenAPI.
    /// Call this from each microservice's Program.cs.
    /// </summary>
    public static IServiceCollection AddFmsSharedKernel(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        // ── Serilog ──
        services.AddFmsLogging(configuration, serviceName);

        // ── OpenTelemetry ──
        services.AddFmsTelemetry(configuration, serviceName, serviceVersion);

        // ── Health Checks ──
        services.AddFmsHealthChecks(configuration);

        // ── JWT Authentication ──
        services.AddFmsAuthentication(configuration);

        // ── OpenAPI / Swagger ──
        services.AddFmsOpenApi(serviceName);

        // ── CORS ──
        services.AddFmsCors();

        // ── Problem Details ──
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddFmsLogging(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("MassTransit", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{ServiceName}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Seq(configuration["Serilog:SeqUrl"] ?? "http://localhost:5341",
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        services.AddSerilog();
        return services;
    }

    public static IServiceCollection AddFmsTelemetry(
        this IServiceCollection services, IConfiguration configuration, string serviceName, string serviceVersion)
    {
        var otelEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health") &&
                                        !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddSource("MassTransit")
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("MassTransit")
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otelEndpoint)));

        return services;
    }

    public static IServiceCollection AddFmsHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecks = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["ready"]);

        var pgConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(pgConnection))
        {
            healthChecks.AddNpgSql(pgConnection, name: "postgresql", tags: ["ready", "db"]);
        }

        var mongoConnection = configuration["MongoDb:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(mongoConnection))
        {
            healthChecks.AddMongoDb(mongoConnection, name: "mongodb", tags: ["ready", "db"]);
        }

        var redisConnection = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            healthChecks.AddRedis(redisConnection, name: "redis", tags: ["ready", "cache"]);
        }

        var rabbitMqConnection = configuration["RabbitMQ:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(rabbitMqConnection))
        {
            healthChecks.AddRabbitMQ(rabbitMqConnection, name: "rabbitmq", tags: ["ready", "messaging"]);
        }

        return services;
    }

    public static IServiceCollection AddFmsAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? "dev-secret-key-change-in-production-fms-2026";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "fms-uat";
        var jwtAudience = configuration["Jwt:Audience"] ?? "fms-clients";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("FMS.Auth");
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddFmsOpenApi(this IServiceCollection services, string serviceName)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = $"FMS {serviceName}", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter your JWT token"
            });
            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }

    public static IServiceCollection AddFmsCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("FmsCors", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:4200",
                        "http://localhost:4201",
                        "https://fms-web-uat.vercel.app",
                        "https://fms-admin-uat.vercel.app",
                        "https://acme-logistics.fms-uat.vercel.app")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        return services;
    }
}

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Registers all SharedKernel middleware pipeline in correct order.
    /// </summary>
    public static IApplicationBuilder UseFmsSharedKernel(this IApplicationBuilder app, IHostEnvironment env)
    {
        // Order matters — outermost first
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseMiddleware<TenantResolutionMiddleware>();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("FmsCors");
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            duration = e.Value.Duration.TotalMilliseconds
                        }),
                        totalDuration = report.TotalDuration.TotalMilliseconds
                    };
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result));
                }
            });

            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });

            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });
        });

        return app;
    }
}
