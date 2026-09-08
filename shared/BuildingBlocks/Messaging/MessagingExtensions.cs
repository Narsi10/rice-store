using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers MassTransit with RabbitMQ and auto-discovers all consumers
    /// in the calling assembly. Every service calls this so they share one
    /// consistent bus configuration.
    /// </summary>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly consumerAssembly)
    {
        // Derive a per-service prefix from the assembly name (e.g. "Payment.API"
        // -> "payment") so each service gets its OWN queue for a given event.
        // Without this, services publishing/consuming the same event type would
        // share one queue and compete for messages (only one would receive each).
        var servicePrefix = consumerAssembly.GetName().Name!
            .Split('.')[0]
            .ToLowerInvariant();

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(servicePrefix, false));
            x.AddConsumers(consumerAssembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var user = configuration["RabbitMq:Username"] ?? "guest";
                var pass = configuration["RabbitMq:Password"] ?? "guest";

                cfg.Host(host, "/", h =>
                {
                    h.Username(user);
                    h.Password(pass);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
