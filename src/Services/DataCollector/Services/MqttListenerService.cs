using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FMS.DataCollector.Services;

/// <summary>
/// Background service that maintains the MQTT connection and reconnects on failure.
/// </summary>
public class MqttListenerService : BackgroundService
{
    private readonly IMqttService _mqttService;
    private readonly ILogger<MqttListenerService> _logger;

    public MqttListenerService(IMqttService mqttService, ILogger<MqttListenerService> logger)
    {
        _mqttService = mqttService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MQTT Listener Service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_mqttService.IsConnected)
            {
                try
                {
                    _logger.LogInformation("Connecting to MQTT broker...");
                    await _mqttService.ConnectAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MQTT connection failed. Retrying in 10 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("MQTT Listener Service stopping...");
    }
}
