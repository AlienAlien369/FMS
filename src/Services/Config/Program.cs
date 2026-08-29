using FMS.Config.Data;
using FMS.Config.Services;
using FMS.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var serviceName = "Config Service";

builder.Services.AddFmsSharedKernel(config, serviceName);

builder.Services.AddDbContext<ConfigDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseFmsSharedKernel(app.Services.GetRequiredService<IHostEnvironment>());

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("⚙️ Config Service started on {Urls}", config["Urls"] ?? "http://localhost:5400");
});

app.Run();
