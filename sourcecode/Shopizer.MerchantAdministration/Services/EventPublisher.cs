using System.Text.Json;
using RabbitMQ.Client;
using Shopizer.MerchantAdministration.Data;
using Shopizer.MerchantAdministration.Models;

namespace Shopizer.MerchantAdministration.Services;

public sealed class EventPublisher(IConnection connection, StoreRepository repository, ILogger<EventPublisher> logger)
{
    // @BR-MSA-VAL-003: StoreCreated is published only after the transaction has durably written the store and outbox row.
    public async Task PublishStoreCreatedAsync(StoreRecord store, RequestContext context, CancellationToken ct)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new { eventId = store.Id.ToString(), eventType = "StoreCreated", eventVersion = 1, occurredAt = DateTimeOffset.UtcNow, tenantId = context.TenantId, storeId = store.Code, correlationId = context.CorrelationId, code = store.Code, name = store.Name, emailAddress = store.EmailAddress, defaultLanguageCode = store.DefaultLanguageCode, supportedLanguageCodes = store.SupportedLanguageCodes });
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await channel.BasicPublishAsync("domain-events", "StoreCreated", false, new BasicProperties { ContentType = "application/json", Persistent = true }, payload, ct);
            await repository.MarkEventPublishedAsync(store.Id, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "StoreCreated publish failed; transactional outbox retained event {StoreId}.", store.Id); }
    }
}
