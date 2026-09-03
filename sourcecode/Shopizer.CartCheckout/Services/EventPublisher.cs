using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shopizer.CartCheckout.Data;
using Shopizer.CartCheckout.Models;

namespace Shopizer.CartCheckout.Services;

public sealed class EventPublisher(IConnection connection, CartRepository repository, ILogger<EventPublisher> logger)
{
    // @BR-CO-ORC-019: The durable OrderSubmitted outbox event is published after the local checkout transaction commits.
    public async Task PublishOrderSubmittedAsync(Guid eventId, RequestContext context, CancellationToken requestCancellation)
        => await PublishAsync(eventId, requestCancellation);

    public async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        var pending = await repository.GetPendingOutboxIdsAsync(20, cancellationToken);
        foreach (var eventId in pending)
            await PublishAsync(eventId, cancellationToken);
    }

    private async Task PublishAsync(Guid eventId, CancellationToken requestCancellation)
    {
        try
        {
            await repository.MarkOutboxAttemptAsync(eventId, requestCancellation);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: requestCancellation);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: requestCancellation);
            // The outbox row is the authoritative durable record; do not reconstruct or
            // publish a second, incomplete representation of the immutable snapshot.
            var payload = await repository.GetOutboxPayloadAsync(eventId, requestCancellation);
            await channel.BasicPublishAsync("domain-events", "OrderSubmitted.v1", false,
                new BasicProperties { ContentType = "application/json", Persistent = true }, payload, requestCancellation);
            await repository.MarkOutboxPublishedAsync(eventId, requestCancellation);
            logger.LogInformation("Published OrderSubmitted.v1 event {EventId}.", eventId);
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested) { throw; }
        catch (NpgsqlException ex) { logger.LogError(ex, "OrderSubmitted.v1 outbox access failed; the event remains durable."); }
        catch (RabbitMQClientException ex) { logger.LogError(ex, "OrderSubmitted.v1 publish failed; the event remains durable."); }
    }
}

public sealed class OutboxDispatcher(EventPublisher publisher, ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await publisher.PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (NpgsqlException ex)
            {
                logger.LogError(ex, "Cart Checkout outbox polling failed.");
            }
            catch (RabbitMQClientException ex)
            {
                logger.LogError(ex, "Cart Checkout outbox dispatch failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
