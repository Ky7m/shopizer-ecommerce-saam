using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shopizer.PlatformIntegrations.Services;

public sealed class IntegrationEventConsumer(
    IConnection connection,
    IServiceScopeFactory scopes,
    ILogger<IntegrationEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        var queue = "platform-integrations-events";
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        foreach (var key in new[] { "BusinessIntegrationDeliveryRequested", "ConfigurationReferenceChanged", "IntegrationDeliveryReplayRequested" })
            await channel.QueueBindAsync(queue, "domain-events", key, cancellationToken: stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                using var document = JsonDocument.Parse(args.Body.ToArray());
                var eventType = document.RootElement.TryGetProperty("eventType", out var type)
                    ? type.GetString() : args.RoutingKey;
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IntegrationService>()
                    .ConsumeEventAsync(eventType ?? args.RoutingKey, document.RootElement, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "MS-12 event consumption failed for {RoutingKey}; message is rejected.", args.RoutingKey);
                await channel.BasicNackAsync(args.DeliveryTag, false, false, stoppingToken);
            }
        };
        await channel.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
