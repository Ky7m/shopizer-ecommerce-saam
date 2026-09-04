using System.Text.Json;
using RabbitMQ.Client;
using Shopizer.PlatformIntegrations.Data;

namespace Shopizer.PlatformIntegrations.Services;

public sealed class EventPublisher(IConnection connection, IntegrationRepository repository, ILogger<EventPublisher> logger)
{
    // @BR-INT-MS12-023: Queued and dead-letter events leave the transactional outbox only after broker publication succeeds.
    public async Task PublishQueuedAsync(Guid eventId, object envelope, CancellationToken ct)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await channel.BasicPublishAsync("domain-events", "IntegrationDeliveryQueued", false,
                new BasicProperties { ContentType = "application/json", Persistent = true },
                JsonSerializer.SerializeToUtf8Bytes(envelope), ct);
            await repository.MarkOutboxPublishedAsync(eventId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "IntegrationDeliveryQueued publication failed; outbox remains durable."); }
    }

    // @BR-INT-MS12-023: Exhausted attempts are published as observable dead-letter events without changing owning business records.
    public async Task PublishDeadLetteredAsync(object envelope, CancellationToken ct)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await channel.BasicPublishAsync("domain-events", "IntegrationDeliveryDeadLettered", false,
                new BasicProperties { ContentType = "application/json", Persistent = true },
                JsonSerializer.SerializeToUtf8Bytes(envelope), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "IntegrationDeliveryDeadLettered publication failed."); }
    }
}
