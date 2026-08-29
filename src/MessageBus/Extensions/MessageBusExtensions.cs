using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FMS.MessageBus.Extensions;

public static class MessageBusExtensions
{
    /// <summary>
    /// Registers MassTransit with RabbitMQ for publishing and consuming events.
    /// </summary>
    public static IServiceCollection AddFmsMessageBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        var rabbitConfig = configuration.GetSection("RabbitMQ");

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            // Configure consumers via callback from each service
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = rabbitConfig["Host"] ?? "localhost";
                var port = ushort.TryParse(rabbitConfig["Port"], out var p) ? p : (ushort)5672;
                var user = rabbitConfig["User"] ?? "guest";
                var pass = rabbitConfig["Password"] ?? "guest";
                var virtualHost = rabbitConfig["VirtualHost"] ?? "/";

                cfg.Host(host, port, virtualHost, h =>
                {
                    h.Username(user);
                    h.Password(pass);
                });

                cfg.UseMessageRetry(r => r.Exponential(3,
                    TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromMilliseconds(500)));

                cfg.UseInMemoryOutbox(context);

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
