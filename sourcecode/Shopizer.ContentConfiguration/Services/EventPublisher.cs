using System.Text.Json;
using RabbitMQ.Client;
using Shopizer.ContentConfiguration.Data;
using Shopizer.ContentConfiguration.Models;

namespace Shopizer.ContentConfiguration.Services;

public sealed class EventPublisher(IConnection connection, ContentRepository repository, ILogger<EventPublisher> logger)
{
    // @BR-EXT-023: Content publication is durable in the outbox and provider failures remain explicit.
    // @BR-EXT-025: Configuration changes publish only a configuration reference, never provider execution data.
    public async Task PublishContentPublishedAsync(ContentRecord content, IEnumerable<ContentDescription> descriptions,
        RequestContext ctx, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var payload = new
        {
            eventId = id,
            eventType = "ContentPublished.v1",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = ctx.TenantId,
            storeId = ctx.StoreId,
            correlationId = ctx.CorrelationId,
            contentId = content.Id,
            code = content.Code,
            contentType = DtoMapper.TitleCase(content.ContentType),
            visible = content.Visible,
            descriptions = descriptions.Select(x => new
            {
                id = x.Id,
                language = x.Language,
                name = x.Name,
                title = x.Title,
                description = x.Description,
                friendlyUrl = x.FriendlyUrl,
                metaKeywords = x.MetaKeywords,
                metaTitle = x.MetaTitle,
                metaDescription = x.MetaDescription
            })
        };
        await PublishAsync(id, "ContentPublished.v1", payload, ctx, ct);
    }

    public async Task PublishConfigurationReferenceChangedAsync(string family, string code, string environment,
        RequestContext ctx, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var payload = new
        {
            eventId = id,
            eventType = "ConfigurationReferenceChanged",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = ctx.TenantId,
            storeId = ctx.StoreId,
            correlationId = ctx.CorrelationId,
            moduleType = "Adapter",
            code,
            environment,
            configurationRef = $"{ctx.TenantId}/{ctx.StoreId}/{family}/{code}",
            version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        };
        await PublishAsync(id, "ConfigurationReferenceChanged", payload, ctx, ct);
    }

    private async Task PublishAsync(Guid id, string type, object payload, RequestContext ctx, CancellationToken ct)
    {
        await repository.WriteOutboxAsync(id, type, ctx, payload, ct);
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            await channel.BasicPublishAsync("domain-events", type, false,
                new BasicProperties { ContentType = "application/json", Persistent = true }, bytes, ct);
            await repository.MarkEventPublishedAsync(id, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "MS-11 event delivery failed; outbox row retained."); }
    }
}
