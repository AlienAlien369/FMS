using FMS.MessageBus.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text.Json;

namespace FMS.DataCollector.Services;

public interface IMqttService
{
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync();
    bool IsConnected { get; }
}

public class MqttService : IMqttService, IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<MqttService> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDeviceAdapterRegistry _adapterRegistry;
    private IMqttClient? _mqttClient;
    private MqttClientOptions? _options;

    public bool IsConnected => _mqttClient?.IsConnected ?? false;

    public async Task DisconnectAsync()
    {
        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.DisconnectAsync();
        }
    }

    public MqttService(
        IConfiguration config,
        ILogger<MqttService> logger,
        IPublishEndpoint publishEndpoint,
        IDeviceAdapterRegistry adapterRegistry)
    {
        _config = config;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _adapterRegistry = adapterRegistry;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var brokerHost = _config["Mqtt:Host"] ?? "localhost";
        var brokerPort = int.TryParse(_config["Mqtt:Port"], out var port) ? port : 1883;
        var username = _config["Mqtt:Username"];
        var password = _config["Mqtt:Password"];

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerHost, brokerPort)
            .WithCredentials(username, password)
            .WithClientId($"fms-datacollector-{Guid.NewGuid():N}")
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

        try
        {
            var response = await _mqttClient.ConnectAsync(_options, ct);
            if (response.ResultCode == MqttClientConnectResultCode.Success)
            {
                _logger.LogInformation("Connected to MQTT broker at {Host}:{Port}", brokerHost, brokerPort);

                // Subscribe to all device topics: {tenantId}/{vendorCode}/{deviceId}/FROM
                await _mqttClient.SubscribeAsync("#", MqttQualityOfServiceLevel.AtLeastOnce, ct);
                _logger.LogInformation("Subscribed to all MQTT topics (#)");
            }
            else
            {
                _logger.LogError("MQTT connection failed: {ResultCode}", response.ResultCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MQTT broker at {Host}:{Port}", brokerHost, brokerPort);
        }
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = System.Text.Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

        _logger.LogDebug("MQTT message on {Topic}: {PayloadLength} bytes", topic, payload.Length);

        try
        {
            // Parse topic: {tenantId}/{vendorCode}/{deviceId}/FROM
            var parts = topic.Split('/');
            if (parts.Length < 4 || parts[^1] != "FROM")
            {
                _logger.LogWarning("Invalid MQTT topic format: {Topic}", topic);
                return;
            }

            var tenantIdStr = parts[0];
            var vendorCode = parts[1];
            var deviceIdStr = parts[2];

            if (!Guid.TryParse(tenantIdStr, out var tenantId))
            {
                _logger.LogWarning("Invalid tenant ID in topic: {TenantId}", tenantIdStr);
                return;
            }

            // Find device by IMEI/serial
            var deviceId = await ResolveDeviceIdAsync(deviceIdStr, tenantId);
            if (!deviceId.HasValue)
            {
                _logger.LogWarning("Unknown device: {DeviceId} for tenant {TenantId}", deviceIdStr, tenantId);
                return;
            }

            // Use device adapter to parse vendor-specific payload
            var adapter = _adapterRegistry.GetAdapter(vendorCode);
            var telemetry = adapter.ParsePayload(payload, tenantId, deviceId.Value, vendorCode);

            // Publish to RabbitMQ for Telemetry Service
            await _publishEndpoint.Publish(new DeviceTelemetryEvent
            {
                DeviceId = deviceId.Value,
                TenantId = tenantId,
                VendorCode = vendorCode,
                Timestamp = telemetry.Timestamp,
                Latitude = telemetry.Latitude,
                Longitude = telemetry.Longitude,
                Speed = telemetry.Speed,
                Heading = telemetry.Heading,
                Ignition = telemetry.Ignition,
                Odometer = telemetry.Odometer,
                FuelLevel = telemetry.FuelLevel,
                Temperature = telemetry.Temperature,
                RawPayload = telemetry.RawPayload
            });

            _logger.LogDebug("Telemetry published for device {DeviceId} ({VendorCode})",
                deviceId.Value, vendorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message on {Topic}", topic);
        }
    }

    private async Task<Guid?> ResolveDeviceIdAsync(string deviceIdOrImei, Guid tenantId)
    {
        // In production, look up from Redis cache → PostgreSQL
        // For now, try parsing as GUID
        if (Guid.TryParse(deviceIdOrImei, out var guid))
            return guid;

        // IMEI lookup (would be cached in Redis)
        // TODO: Implement IMEI → DeviceId lookup
        return null;
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        _logger.LogWarning("Disconnected from MQTT broker. Reconnecting in 5 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(5));

        try
        {
            await _mqttClient!.ConnectAsync(_options!, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect to MQTT broker");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.DisconnectAsync();
        }
        _mqttClient?.Dispose();
    }
}
