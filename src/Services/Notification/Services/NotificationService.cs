using FMS.MessageBus.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FMS.Notification.Services;

// ──────────────────────────────────────
// Provider interfaces
// ──────────────────────────────────────

public interface IEmailProvider
{
    Task<bool> SendAsync(string to, string subject, string htmlBody, string? from = null);
}

public interface ISmsProvider
{
    Task<bool> SendAsync(string to, string message);
}

public interface IPushProvider
{
    Task<bool> SendAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
}

public interface INotificationService
{
    Task SendAsync(SendNotificationEvent notification);
}

// ──────────────────────────────────────
// SendGrid Email Provider
// ──────────────────────────────────────

public class SendGridEmailProvider : IEmailProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailProvider> _logger;
    private readonly HttpClient _httpClient;

    public SendGridEmailProvider(IConfiguration config, ILogger<SendGridEmailProvider> logger, HttpClient httpClient)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> SendAsync(string to, string subject, string htmlBody, string? from = null)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SendGrid API key not configured. Email to {To} not sent.", to);
            return false;
        }

        var fromEmail = from ?? _config["SendGrid:FromEmail"] ?? "noreply@fms-uat.com";
        var fromName = _config["SendGrid:FromName"] ?? "FMS Fleet Management";

        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = to } }, subject } },
            from = new { email = fromEmail, name = fromName },
            subject,
            content = new[] { new { type = "text/html", value = htmlBody } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
        {
            Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        try
        {
            var response = await _httpClient.SendAsync(request);
            _logger.LogInformation("Email sent to {To}: {StatusCode}", to, response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}

// ──────────────────────────────────────
// Twilio SMS Provider
// ──────────────────────────────────────

public class TwilioSmsProvider : ISmsProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(IConfiguration config, ILogger<TwilioSmsProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<bool> SendAsync(string to, string message)
    {
        var accountSid = _config["Twilio:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid))
        {
            _logger.LogWarning("Twilio not configured. SMS to {To} not sent.", to);
            return Task.FromResult(false);
        }

        // In production, use Twilio SDK
        _logger.LogInformation("SMS to {To}: {Message}", to, message);
        return Task.FromResult(true);
    }
}

// ──────────────────────────────────────
// Push Notification Provider (FCM)
// ──────────────────────────────────────

public class FcmPushProvider : IPushProvider
{
    private readonly ILogger<FcmPushProvider> _logger;

    public FcmPushProvider(ILogger<FcmPushProvider> logger) => _logger = logger;

    public Task<bool> SendAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
    {
        // In production, use Firebase Admin SDK
        _logger.LogInformation("Push to {Token}: {Title} - {Body}", deviceToken, title, body);
        return Task.FromResult(true);
    }
}

// ──────────────────────────────────────
// Notification Service (orchestrator)
// ──────────────────────────────────────

public class NotificationService : INotificationService
{
    private readonly IEmailProvider _email;
    private readonly ISmsProvider _sms;
    private readonly IPushProvider _push;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEmailProvider email, ISmsProvider sms, IPushProvider push, ILogger<NotificationService> logger)
    {
        _email = email;
        _sms = sms;
        _push = push;
        _logger = logger;
    }

    public async Task SendAsync(SendNotificationEvent notification)
    {
        var data = notification.TemplateData ?? new Dictionary<string, string>();
        var template = GetTemplate(notification.TemplateKey, data);

        _logger.LogInformation("Sending {Channel} notification to {Recipient} (tenant {TenantId})",
            notification.Channel, notification.Recipient, notification.TenantId);

        bool success = notification.Channel switch
        {
            "email" => await _email.SendAsync(notification.Recipient, template.Subject, template.HtmlBody),
            "sms" => await _sms.SendAsync(notification.Recipient, template.PlainBody),
            "push" => await _push.SendAsync(notification.Recipient, template.Subject, template.PlainBody, data),
            _ => false
        };

        if (!success)
            _logger.LogWarning("Failed to send {Channel} notification to {Recipient}", notification.Channel, notification.Recipient);
    }

    private static NotificationTemplate GetTemplate(string key, Dictionary<string, string> data)
    {
        return key switch
        {
            "alert_overspeed" => new(
                "⚠️ Overspeed Alert",
                $"<h2>Overspeed Alert</h2><p>A vehicle is overspeeding at <strong>{data.GetValueOrDefault("speed", "N/A")} km/h</strong>.</p><p>Severity: <strong>{data.GetValueOrDefault("severity", "N/A")}</strong></p>",
                $"Overspeed Alert: {data.GetValueOrDefault("speed", "N/A")} km/h"),
            "alert_panic" => new(
                "🚨 PANIC ALERT",
                $"<h1 style='color:red'>🚨 PANIC BUTTON ACTIVATED</h1><p>A panic button has been triggered. Immediate response required.</p>",
                $"PANIC ALERT - Immediate response required"),
            "alert_low_fuel" => new(
                "⛽ Low Fuel Warning",
                $"<h2>Low Fuel Warning</h2><p>Fuel level: <strong>{data.GetValueOrDefault("message", "N/A")}</strong></p>",
                $"Low fuel: {data.GetValueOrDefault("message", "N/A")}"),
            _ => new($"FMS: {key}", $"<p>{data.GetValueOrDefault("message", key)}</p>", data.GetValueOrDefault("message", key))
        };
    }
}

public record NotificationTemplate(string Subject, string HtmlBody, string PlainBody);

// ──────────────────────────────────────
// MassTransit Consumer
// ──────────────────────────────────────

public class NotificationEventConsumer : IConsumer<SendNotificationEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationEventConsumer> _logger;

    public NotificationEventConsumer(INotificationService notificationService, ILogger<NotificationEventConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        _logger.LogInformation("Processing notification: {Channel} to {Recipient}",
            context.Message.Channel, context.Message.Recipient);

        await _notificationService.SendAsync(context.Message);
    }
}
