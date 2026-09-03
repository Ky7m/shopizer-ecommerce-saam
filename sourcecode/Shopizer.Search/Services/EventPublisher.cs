using System.Text.Json;
using Npgsql;
using RabbitMQ.Client;

namespace Shopizer.Search.Services;

public sealed class EventPublisher(
    IConnection connection,
    NpgsqlDataSource dataSource,
    ILogger<EventPublisher> logger)
{
    public async Task PublishPendingAsync(Guid eventId, CancellationToken ct)
    {
        string? eventType = null;
        string? payload = null;
        await using (var database = await dataSource.OpenConnectionAsync(ct))
        await using (var command = new NpgsqlCommand("""
            SELECT event_type, payload::text
            FROM search.event_outbox
            WHERE id=@id AND published_at IS NULL
            """, database))
        {
            command.Parameters.AddWithValue("id", eventId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                eventType = reader.GetString(0);
                payload = reader.GetString(1);
            }
        }

        if (eventType is null || payload is null)
        {
            return;
        }

        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("operational-events", ExchangeType.Topic,
                durable: true, autoDelete: false, cancellationToken: ct);
            await channel.BasicPublishAsync(
                "operational-events", eventType, false,
                new BasicProperties { ContentType = "application/json", Persistent = true },
                JsonSerializer.SerializeToUtf8Bytes(JsonDocument.Parse(payload).RootElement), ct);

            await using var database = await dataSource.OpenConnectionAsync(ct);
            await using var mark = new NpgsqlCommand(
                "UPDATE search.event_outbox SET published_at=now() WHERE id=@id AND published_at IS NULL",
                database);
            mark.Parameters.AddWithValue("id", eventId);
            await mark.ExecuteNonQueryAsync(ct);
            logger.LogInformation("Published search operational event {EventType} {EventId}.", eventType, eventId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search event {EventId} was not delivered; the outbox remains durable.", eventId);
        }
    }
}
