using System.Text.Json;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Shopizer.OrderManagement.Data;
using Shopizer.OrderManagement.Models;

namespace Shopizer.OrderManagement.Services;

public sealed class EventPublisher(IConnection connection, OrderRepository repository, ILogger<EventPublisher> logger)
{
    // @BR-OR-RES-001: Outbox rows are durable before at-least-once RabbitMQ publication is attempted.
    public async Task PublishPendingAsync(CancellationToken ct)
    {
        foreach (var eventId in await repository.PendingOutboxAsync(50, ct))
            await PublishAsync(eventId, ct);
    }

    // @BR-OR-PAY-001: Payment commands leave MS-05 as durable events and provider execution remains owned by MS-06.
    public Task EnqueueCommandAsync(RequestContext context, long orderId, string type, object payload, string key, CancellationToken ct) =>
        repository.EnqueueCommandAsync(context, orderId, type, payload, key, ct);

    // @BR-OR-FAIL-001: A failed submission leaves a durable processing-failure event for compensation and retry.
    public Task RecordProcessingFailureAsync(RequestContext context, string submissionId, string reason, CancellationToken ct) =>
        repository.RecordProcessingFailureAsync(context, submissionId, reason, ct);

    private async Task PublishAsync(Guid eventId, CancellationToken ct)
    {
        try
        {
            await repository.MarkOutboxAttemptAsync(eventId, ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            var payload = await repository.OutboxPayloadAsync(eventId, ct);
            var eventType = await repository.OutboxEventTypeAsync(eventId, ct);
            await channel.BasicPublishAsync("domain-events", eventType, false,
                new BasicProperties { ContentType = "application/json", Persistent = true }, payload, ct);
            await repository.MarkOutboxPublishedAsync(eventId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (NpgsqlException ex) { logger.LogError(ex, "Order outbox remains durable after database dispatch failure."); }
        catch (RabbitMQClientException ex) { logger.LogError(ex, "Order outbox remains durable after RabbitMQ dispatch failure."); }
    }
}

public sealed class OutboxDispatcher(EventPublisher publisher, ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await publisher.PublishPendingAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogError(ex, "Order outbox polling failed."); }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public sealed class OrderEventConsumer(IConnection connection, IServiceScopeFactory scopes, ILogger<OrderEventConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
                await channel.ExchangeDeclareAsync("domain-events-dlx", ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: stoppingToken);
                var arguments = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "domain-events-dlx" };
                await channel.QueueDeclareAsync("order-management-events", durable: true, exclusive: false, autoDelete: false, arguments, cancellationToken: stoppingToken);
                foreach (var key in new[] { "OrderSubmitted.v1", "PaymentAuthorized.v1", "PaymentCaptured.v1", "PaymentFailed.v1", "PaymentRefunded.v1", "PaymentVoided.v1", "ShipmentStatusUpdated", "InventoryReservationReleased" })
                    await channel.QueueBindAsync("order-management-events", "domain-events", key, cancellationToken: stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, delivery) =>
                {
                    try
                    {
                        using var document = JsonDocument.Parse(delivery.Body);
                        var body = document.RootElement;
                        var eventType = body.TryGetProperty("eventType", out var type) ? type.GetString() ?? delivery.RoutingKey : delivery.RoutingKey;
                        var tenant = body.TryGetProperty("tenantId", out var tenantValue) ? tenantValue.GetString() : null;
                        var store = body.TryGetProperty("storeId", out var storeValue) ? storeValue.GetString() : null;
                        var correlation = body.TryGetProperty("correlationId", out var correlationValue) ? correlationValue.GetString() : null;
                        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store))
                            throw new InvalidOperationException("An order event is missing tenant/store metadata.");
                        var context = new RequestContext(tenant, store, string.IsNullOrWhiteSpace(correlation) ? delivery.BasicProperties.MessageId ?? delivery.DeliveryTag.ToString() : correlation);
                        using var scope = scopes.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<OrderService>();
                        await service.ApplyEventAsync(eventType, body, context, stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Order event was rejected and sent to the dead-letter exchange.");
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, false, stoppingToken);
                    }
                };
                await channel.BasicConsumeAsync("order-management-events", false, consumer, stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Order event consumer unavailable; retrying.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
