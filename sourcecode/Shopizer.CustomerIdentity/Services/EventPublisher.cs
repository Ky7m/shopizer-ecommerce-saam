using System.Text.Json;
using RabbitMQ.Client;
using Shopizer.CustomerIdentity.Data;
using Shopizer.CustomerIdentity.Models;

namespace Shopizer.CustomerIdentity.Services;

public sealed class EventPublisher(IConnection connection, IdentityRepository repository, ILogger<EventPublisher> logger)
{
    // @BR-CUS-002: Registration emits the approved CustomerRegistered domain event after durable persistence.
    public async Task PublishCustomerRegisteredAsync(CustomerAccount customer, RequestContext context, CancellationToken ct)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventId = customer.Id.ToString(),
            eventType = "CustomerRegistered",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            customerId = customer.Id,
            loginName = customer.LoginName,
            emailAddress = customer.EmailAddress,
            status = customer.Status
        });
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await channel.BasicPublishAsync("domain-events", "CustomerRegistered", false, new BasicProperties { ContentType = "application/json", Persistent = true }, payload, ct);
            await repository.MarkEventPublishedAsync(customer.Id, ct);
            logger.LogInformation("Published CustomerRegistered for customer {CustomerId}.", customer.Id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            // The durable event_outbox row written in the same registration transaction remains
            // available for the deployment's relay when RabbitMQ is temporarily unavailable.
            logger.LogError(ex, "CustomerRegistered publish failed; the transactional outbox retains the event.");
        }
    }
}
