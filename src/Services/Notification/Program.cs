using FMS.Notification.Services;
using FMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var serviceName = "Notification Service";

builder.Services.AddFmsSharedKernel(config, serviceName);

// ── HTTP client for SendGrid ──
builder.Services.AddHttpClient<IEmailProvider, SendGridEmailProvider>();

// ── Notification providers ──
builder.Services.AddScoped<ISmsProvider, TwilioSmsProvider>();
builder.Services.AddSingleton<IPushProvider, FcmPushProvider>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddControllers();

var app = builder.Build();
app.UseFmsSharedKernel(app.Services.GetRequiredService<IHostEnvironment>());
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("🔔 Notification Service started on {Urls}", config["Urls"] ?? "http://localhost:5500");
});

app.Run();
