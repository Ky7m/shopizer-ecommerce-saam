using System.Text.Json;
using RabbitMQ.Client;
using Shopizer.Payments.Data;
using Shopizer.Payments.Models;

namespace Shopizer.Payments.Services;

public sealed class EventPublisher(IConnection connection, PaymentRepository repository, ILogger<EventPublisher> logger)
{
    // @BR-ORD-015: Provider outcomes are delivered as durable payment events after the local transaction commits.
    // @BR-EXT-002: Capture publishes PaymentCaptured and leaves order lifecycle ownership with MS-05.
    public async Task PublishAsync(Guid aggregateId, string eventType, object payload, RequestContext context, CancellationToken ct)
    {
        var envelope = JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventId = Guid.NewGuid(),
            eventType,
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            payload
        });
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await channel.BasicPublishAsync("domain-events", eventType, false,
                new BasicProperties { ContentType = "application/json", Persistent = true }, envelope, ct);
            await repository.MarkOutboxPublishedAsync(aggregateId, eventType, context, ct);
            logger.LogInformation("Published {EventType} for payment aggregate {AggregateId}.", eventType, aggregateId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment event {EventType} publish failed; transactional outbox remains pending.", eventType);
        }
    }
}
