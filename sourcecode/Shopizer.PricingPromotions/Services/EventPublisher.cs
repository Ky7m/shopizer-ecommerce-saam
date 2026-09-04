using System.Text.Json;
using RabbitMQ.Client;
using Shopizer.PricingPromotions.Data;
using Shopizer.PricingPromotions.Models;

namespace Shopizer.PricingPromotions.Services;

public sealed class EventPublisher(
    IConnection connection,
    PricingRepository repository,
    ILogger<EventPublisher> logger)
{
    // @BR-PRC-002: Every durable price mutation publishes the approved PriceChanged.v1 event.
    public async Task PublishPriceChangedAsync(
        PriceEntry price, string changeType, Guid eventId, RequestContext context, CancellationToken ct)
    {
        var payload = new
        {
            eventId,
            eventType = "PriceChanged.v1",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            productSku = price.ProductSku,
            variantSku = price.VariantSku,
            priceId = price.Id,
            changeType,
            amount = price.Amount,
            priceType = price.PriceType,
            defaultPrice = price.DefaultPrice,
            specialAmount = price.SpecialAmount
        };
        await PublishAsync("PriceChanged.v1", eventId, payload, context, ct);
    }

    // @BR-PRC-008: Promotion changes use the same durable event path when promotion administration is enabled.
    public async Task PublishPromotionChangedAsync(
        Promotion promotion, string changeType, Guid eventId, RequestContext context, CancellationToken ct)
    {
        var payload = new
        {
            eventId,
            eventType = "PromotionChanged.v1",
            eventVersion = 1,
            occurredAt = DateTimeOffset.UtcNow,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            promotionId = promotion.Id,
            promoCode = promotion.RuleKey,
            changeType,
            name = promotion.Name,
            ruleKey = promotion.RuleKey,
            discountRate = promotion.DiscountRate,
            validFrom = promotion.ValidFrom?.ToString("yyyy-MM-dd"),
            validUntil = promotion.ValidUntil?.ToString("yyyy-MM-dd"),
            isEnabled = promotion.IsEnabled
        };
        await PublishAsync("PromotionChanged.v1", eventId, payload, context, ct);
    }

    private async Task PublishAsync(
        string routingKey, Guid eventId, object payload, RequestContext context, CancellationToken ct)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true,
                autoDelete: false, cancellationToken: ct);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            await channel.BasicPublishAsync("domain-events", routingKey, false,
                new BasicProperties { ContentType = "application/json", Persistent = true }, bytes, ct);
            await repository.MarkEventPublishedAsync(eventId, ct);
            logger.LogInformation("Published {EventType} for tenant {TenantId}, store {StoreId}.",
                routingKey, context.TenantId, context.StoreId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The mutation transaction already committed its outbox row. A relay can retry it.
            logger.LogError(ex, "Publishing {EventType} failed; the transactional outbox retains the event.",
                routingKey);
        }
    }
}
