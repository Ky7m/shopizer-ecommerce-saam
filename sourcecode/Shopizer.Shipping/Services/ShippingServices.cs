using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shopizer.Shipping.Data;
using Shopizer.Shipping.DTOs;
using Shopizer.Shipping.Models;

namespace Shopizer.Shipping.Services;

public sealed record TokenData(Guid SubjectId, string Kind, string Login, string TenantId, string StoreId,
    DateTimeOffset ExpiresAt, IReadOnlyList<string> Roles);

public sealed class TokenService(IConfiguration configuration, IHostEnvironment environment)
{
    private readonly byte[] secret = CreateSecret(configuration, environment);

    private static byte[] CreateSecret(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Shipping:JwtSecret"];
        if (!string.IsNullOrWhiteSpace(configured)) return Encoding.UTF8.GetBytes(configured);
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Shipping:JwtSecret must be configured outside Development.");
        return RandomNumberGenerator.GetBytes(64);
    }

    public TokenData? Validate(string raw, RequestContext context)
    {
        try
        {
            var pieces = raw.Split('.');
            if (pieces.Length != 3) return null;
            using var hmac = new HMACSHA512(secret);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{pieces[0]}.{pieces[1]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, Decode(pieces[2]))) return null;
            using var json = JsonDocument.Parse(Decode(pieces[1]));
            var root = json.RootElement;
            if (root.GetProperty("aud").GetString() != "api") return null;
            var tenant = root.GetProperty("tenantId").GetString()!;
            var store = root.GetProperty("storeId").GetString()!;
            if (!tenant.Equals(context.TenantId, StringComparison.Ordinal) ||
                !store.Equals(context.StoreId, StringComparison.Ordinal)) return null;
            var expiry = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64());
            if (expiry <= DateTimeOffset.UtcNow) return null;
            var subject = Guid.Parse(root.GetProperty("sub").GetString()!);
            var roles = root.TryGetProperty("roles", out var roleJson)
                ? roleJson.EnumerateArray().Select(x => x.GetString() ?? "").ToArray()
                : [];
            return new TokenData(subject, root.GetProperty("kind").GetString()!,
                root.GetProperty("name").GetString()!, tenant, store, expiry, roles);
        }
        catch (FormatException) { return null; }
        catch (JsonException) { return null; }
        catch (CryptographicException) { return null; }
        catch (KeyNotFoundException) { return null; }
    }

    private static byte[] Decode(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
            new string('=', (4 - value.Length % 4) % 4));
}

public sealed class EventPublisher(IConnection connection, ShippingRepository repository,
    ILogger<EventPublisher> logger)
{
    // @BR-PRC-028: Persisted quote snapshots emit a durable adapter request after the transaction commits.
    public async Task PublishAdapterExecutionRequestedAsync(ShippingQuoteRecord quote, RequestContext context,
        CancellationToken ct)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            eventId = quote.Id,
            eventType = "ShippingAdapterExecutionRequested.v1",
            eventVersion = 1,
            occurredAt = quote.QuotedAt,
            tenantId = context.TenantId,
            storeId = context.StoreId,
            correlationId = context.CorrelationId,
            requestType = "CarrierQuote",
            providerCode = quote.ProviderCode
        });
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync("domain-events", ExchangeType.Topic, durable: true,
                autoDelete: false, cancellationToken: ct);
            await channel.BasicPublishAsync("domain-events",
                "ShippingAdapterExecutionRequested.v1", false,
                new BasicProperties { ContentType = "application/json", Persistent = true }, payload, ct);
            await repository.MarkEventPublishedAsync(quote.Id, ct);
            logger.LogInformation("Published shipping adapter request for quote {QuoteId}.", quote.Id);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Shipping adapter request publish failed; transactional outbox retained.");
        }
    }
}

public sealed class ShippingService(ShippingRepository repository, EventPublisher events)
{
    private static readonly HashSet<string> ProviderCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "canadapost", "usps", "ups", "fedex", "priceByDistance", "customWeight", "customQuotesRules", "storePickUp"
    };

    public async Task<ShippingConfigurationRecord> GetConfigurationAsync(RequestContext context, CancellationToken ct) =>
        await repository.GetConfigurationAsync(context, ct);

    public async Task<List<ShippingModuleSummaryDto>> ListModulesAsync(RequestContext context, CancellationToken ct)
    {
        var modules = await repository.ListModulesAsync(context, ct);
        return modules.Select(x => new ShippingModuleSummaryDto
        {
            Code = x.ModuleCode,
            Configured = true,
            Active = x.Active,
            Image = null
        }).ToList();
    }

    public async Task<ShippingModuleRecord> GetModuleAsync(string code, RequestContext context, CancellationToken ct) =>
        await repository.GetModuleAsync(code, context, ct) ??
        throw new DomainException("MODULE_NOT_FOUND", "Shipping module is not configured for this store", 404);

    // @BR-UI-008: Shipping module commands preserve ordered integration values and validate the provider registry.
    public async Task<ShippingModuleRecord> SaveModuleAsync(ShippingModuleConfigurationRequestDto request,
        RequestContext context, CancellationToken ct)
    {
        ValidateModule(request.ModuleCode, request.Environment);
        var keys = request.IntegrationKeys?.ToDictionary(x => x.Key, x => x.Value?.ToString(),
            StringComparer.OrdinalIgnoreCase) ?? new(StringComparer.OrdinalIgnoreCase);
        var options = request.IntegrationOptions?.ToDictionary(x => x.Key, x => x.Value,
            StringComparer.OrdinalIgnoreCase) ?? new(StringComparer.OrdinalIgnoreCase);
        ValidateDistanceBands(options);
        return await repository.SaveModuleAsync(new ShippingModuleRecord
        {
            ModuleCode = request.ModuleCode,
            Active = request.Active,
            DefaultSelected = request.DefaultSelected,
            Environment = request.Environment,
            IntegrationKeys = keys,
            IntegrationOptions = options
        }, context, ct);
    }

    public async Task<ShippingOriginRecord> GetOriginAsync(RequestContext context, CancellationToken ct) =>
        await repository.GetOriginAsync(context, ct) ??
        throw new DomainException("ORIGIN_NOT_FOUND", "A shipping origin is not configured", 404);

    // @BR-PRC-022: An active origin is scoped to the tenant and store and replaces the prior active origin.
    public async Task<ShippingOriginRecord> SaveOriginAsync(ShippingOriginRequestDto request,
        RequestContext context, CancellationToken ct)
    {
        ValidateCountry(request.CountryCode, "countryCode");
        if (string.IsNullOrWhiteSpace(request.Address) || string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.PostalCode))
            throw new DomainException("ORIGIN_UNAVAILABLE", "A shipping origin requires address, city and postal code", 422);
        return await repository.SaveOriginAsync(new ShippingOriginRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            StoreId = context.StoreId,
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            PostalCode = request.PostalCode.Trim(),
            State = request.State,
            CountryCode = request.CountryCode,
            ZoneCode = request.ZoneCode,
            Active = request.Active
        }, context, ct);
    }

    public async Task<List<ShippingPackageRecord>> ListPackagesAsync(RequestContext context, CancellationToken ct) =>
        await repository.ListPackagesAsync(context, ct);

    public async Task<ShippingPackageRecord> GetPackageAsync(string code, RequestContext context, CancellationToken ct) =>
        await repository.GetPackageAsync(code, context, ct) ??
        throw new DomainException("PACKAGE_NOT_FOUND", "Package code is not configured", 404);

    // @BR-PRC-029: Package mode is restricted to ITEM or BOX and defaults to ITEM at the orchestration boundary.
    // @BR-PRC-031: Box definitions reject invalid capacity and products that exceed dimensions, weight, or volume.
    public async Task<ShippingPackageRecord> SavePackageAsync(ShippingPackageRequestDto request, string? code,
        RequestContext context, CancellationToken ct)
    {
        ValidatePackage(request);
        var existing = code is null ? null : await repository.GetPackageAsync(code, context, ct);
        if (code is not null && existing is null)
            throw new DomainException("PACKAGE_NOT_FOUND", "Package code is not configured", 404);
        return await repository.SavePackageAsync(new ShippingPackageRecord
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            TenantId = context.TenantId,
            StoreId = context.StoreId,
            Code = code ?? request.Code,
            ShippingWidth = request.ShippingWidth,
            ShippingHeight = request.ShippingHeight,
            ShippingLength = request.ShippingLength,
            ShippingWeight = request.ShippingWeight,
            ShippingMaxWeight = request.ShippingMaxWeight,
            Treshold = request.Treshold,
            Type = request.Type,
            DefaultPackaging = request.DefaultPackaging
        }, context, ct);
    }

    public async Task DeletePackageAsync(string code, RequestContext context, CancellationToken ct)
    {
        if (!await repository.DeletePackageAsync(code, context, ct))
            throw new DomainException("PACKAGE_NOT_FOUND", "Package code is not configured", 404);
    }

    public async Task<ExpeditionConfigurationRecord> GetExpeditionAsync(RequestContext context, CancellationToken ct) =>
        await repository.GetExpeditionAsync(context, ct) ?? new ExpeditionConfigurationRecord();

    // @BR-PRC-023: National and international destination lists are validated before a quote provider can run.
    public async Task<ExpeditionConfigurationRecord> SaveExpeditionAsync(ExpeditionConfigurationRequestDto request,
        RequestContext context, CancellationToken ct)
    {
        var countries = request.ShipToCountry ?? [];
        foreach (var country in countries) ValidateCountry(country, "shipToCountry");
        return await repository.SaveExpeditionAsync(new ExpeditionConfigurationRecord
        {
            InternationalShipping = request.InternationalShipping!.Value,
            TaxOnShipping = request.TaxOnShipping!.Value,
            ShipToCountry = countries.Distinct(StringComparer.Ordinal).ToList()
        }, context, ct);
    }

    public async Task<List<ShippingCountryDto>> ListCountriesAsync(RequestContext context, string? language,
        CancellationToken ct)
    {
        var expedition = await GetExpeditionAsync(context, ct);
        var origin = await repository.GetOriginAsync(context, ct);
        var codes = expedition.InternationalShipping
            ? expedition.ShipToCountry
            : origin is null ? [] : [origin.CountryCode];
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CA"] = "Canada",
            ["US"] = "United States",
            ["GB"] = "United Kingdom",
            ["FR"] = "France",
            ["DE"] = "Germany",
            ["AU"] = "Australia",
            ["JP"] = "Japan"
        };
        return codes.Distinct(StringComparer.OrdinalIgnoreCase).Select(code => new ShippingCountryDto
        {
            Code = code,
            Name = names.TryGetValue(code, out var name) ? name : code,
            Language = string.IsNullOrWhiteSpace(language) ? "en" : language
        }).ToList();
    }

    // @BR-PRC-022: The effective origin is resolved before any destination or provider operation.
    // @BR-PRC-023: National and international destination eligibility prevents unsupported quotes.
    // @BR-PRC-024: The first active non-preprocessor module is selected in registry order.
    // @BR-PRC-025: Active preprocessors run before provider quotation and may replace the provider only when configured.
    // @BR-PRC-026: Free shipping applies only when merchandise total is strictly greater than its threshold.
    // @BR-PRC-027: Option selection compares exact decimal prices using the configured policy.
    // @BR-PRC-028: Each final option is stored as an immutable tenant/store-scoped quote snapshot.
    // @BR-PRC-029: Quote packaging uses configured ITEM or BOX mode, defaulting to ITEM.
    // @BR-PRC-030: Virtual products are excluded and missing package facts receive the specified defaults.
    // @BR-PRC-031: Box fitting enforces dimensions, weight, and the seventy-five-percent volume rule.
    // @BR-PRC-032: Generated box facts report each box's own accumulated weight plus empty-box weight.
    // @BR-PRC-033: Distance pricing uses the <=20 rate band and a 150 km service radius.
    // @BR-PRC-034: Distance enrichment is requested only for allowed zones with a postal code.
    // @BR-PRC-035: Package weight, volume, size, destination, and distance drive provider decisions.
    // @BR-PRC-036: Distance bands are deterministic and overlapping unprioritized bands are rejected.
    // @BR-EXT-010: Destination policy is completed before an adapter request is emitted.
    // @BR-EXT-011: Eligible free shipping bypasses provider quotation and returns zero cost.
    // @BR-EXT-012: MS-09 owns provider replacement policy and sends the selected code to adapters.
    // @BR-EXT-013: Distance providers never fabricate a price without a usable adapter distance.
    // @BR-EXT-015: Custom weight brackets are checked in configuration order for the first match.
    // @BR-EXT-016: Active pickup configuration contributes an option through preprocessing.
    // @BR-EXT-018: Persisted quotes expose stable provider, option, price, handling, and delivery facts.
    public async Task<ShippingSummaryResult> CalculateAsync(string cart, ShippingAddressRequestDto request,
        RequestContext context, string? ipAddress, CancellationToken ct)
    {
        ValidateCountry(request.CountryCode, "countryCode");
        if (string.IsNullOrWhiteSpace(request.PostalCode))
            throw new DomainException("INVALID_REQUEST", "postalCode is required", 400);

        var origin = await repository.GetOriginAsync(context, ct) ??
            throw new DomainException("ORIGIN_UNAVAILABLE", "A shipping origin is required", 422);
        var expedition = await GetExpeditionAsync(context, ct);
        var destination = new DeliveryAddressDto
        {
            CountryCode = request.CountryCode,
            PostalCode = request.PostalCode,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZoneCode = request.ZoneCode
        };
        if (!expedition.InternationalShipping &&
            !origin.CountryCode.Equals(destination.CountryCode, StringComparison.OrdinalIgnoreCase))
            return NoShipping(destination, "NO_SHIPPING_TO_SELECTED_COUNTRY");
        if (expedition.InternationalShipping && expedition.ShipToCountry.Count > 0 &&
            !expedition.ShipToCountry.Contains(destination.CountryCode, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("DESTINATION_NOT_SUPPORTED",
                $"Shipping is not available to {destination.CountryCode}", 422);

        var config = await repository.GetConfigurationAsync(context, ct);
        var modules = await repository.ListModulesAsync(context, ct);
        var activeModules = modules.Where(x => x.Active).ToList();
        var preprocessorOptions = new List<ShippingOptionResult>();
        foreach (var processor in activeModules.Where(x => IsPreprocessor(x.ModuleCode)))
            ApplyPreprocessor(processor, destination, preprocessorOptions);

        var total = ParseDecimal(activeModules.SelectMany(x => x.IntegrationKeys)
            .Where(x => x.Key.Equals("cartTotal", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value).FirstOrDefault()) ?? 0m;
        if (config.FreeShippingEnabled && config.OrderTotalFreeShipping is decimal threshold &&
            total > threshold && (config.FreeShippingType is null ||
                config.FreeShippingType.Equals("International", StringComparison.OrdinalIgnoreCase) ||
                origin.CountryCode.Equals(destination.CountryCode, StringComparison.OrdinalIgnoreCase)))
        {
            var free = new ShippingOptionResult
            {
                OptionPrice = 0,
                OptionPriceText = "0.00",
                OptionCode = "FREE_SHIPPING",
                OptionId = $"FREE_SHIPPING_{destination.CountryCode}",
                OptionName = "Free shipping",
                ShippingModuleCode = "shipping-policy"
            };
            preprocessorOptions.Add(free);
            var freeSummary = Summary(destination, config, preprocessorOptions, free, new()
            {
                ["merchandiseTotal"] = total,
                ["freeShippingThreshold"] = threshold
            });
            await PersistAndPublishAsync(cart, freeSummary, context, ipAddress, ct,
                publishAdapterRequest: false);
            return freeSummary;
        }

        var provider = SelectProvider(activeModules);
        if (provider is null && preprocessorOptions.Count == 0)
            throw new DomainException("NO_SHIPPING_MODULE_CONFIGURED",
                "No active shipping provider is configured", 422);

        var replacementCode = activeModules.Where(x => IsPreprocessor(x.ModuleCode))
            .Select(x => x.IntegrationKeys.GetValueOrDefault("replacementProvider"))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        if (!string.IsNullOrWhiteSpace(replacementCode))
        {
            var replacement = activeModules.FirstOrDefault(x =>
                x.ModuleCode.Equals(replacementCode, StringComparison.OrdinalIgnoreCase));
            if (replacement is null || !replacement.Active)
                throw new DomainException("PROVIDER_REPLACEMENT_UNAVAILABLE",
                    "Selected shipping provider is not active", 422);
            provider = replacement;
        }
        var facts = BuildPackageFacts(provider, config);
        if (facts.Count == 0)
            return Summary(destination, config, preprocessorOptions, null, new() { ["shippingRequired"] = false });
        var distance = ParseDecimal(provider?.IntegrationKeys.GetValueOrDefault("distanceKm"));
        var decision = DecideProvider(facts, destination, distance, activeModules);
        if (decision is not null && !string.Equals(provider?.ModuleCode, decision, StringComparison.OrdinalIgnoreCase))
        {
            provider = activeModules.FirstOrDefault(x => x.ModuleCode.Equals(decision, StringComparison.OrdinalIgnoreCase));
            if (provider is null)
                throw new DomainException("DECISION_PROVIDER_UNAVAILABLE",
                    $"{decision} is not configured", 422);
        }

        var options = new List<ShippingOptionResult>(preprocessorOptions);
        if (provider is not null && !IsPreprocessor(provider.ModuleCode))
            options.Add(await QuoteWithConfiguredAdapterAsync(provider, destination, facts, distance, ct));
        if (options.Count == 0)
            throw new DomainException(provider?.ModuleCode.Equals("priceByDistance", StringComparison.OrdinalIgnoreCase) == true
                ? "DISTANCE_UNAVAILABLE" : "PROVIDER_ERROR",
                provider?.ModuleCode.Equals("priceByDistance", StringComparison.OrdinalIgnoreCase) == true
                    ? "Distance-based shipping cannot be calculated" : "The shipping provider returned no options",
                provider?.ModuleCode.Equals("priceByDistance", StringComparison.OrdinalIgnoreCase) == true ? 422 : 502);

        var selected = SelectOptions(options, config.ShippingOptionPriceType);
        var audit = new Dictionary<string, object?>
        {
            ["totalWeight"] = facts.Sum(x => x.Weight),
            ["largestVolume"] = facts.Max(x => x.Height * x.Length * x.Width),
            ["largestDimension"] = facts.Max(x => Math.Max(x.Height, Math.Max(x.Length, x.Width))),
            ["distanceKm"] = distance,
            ["appliedRate"] = distance is null ? null : distance <= 20 ? 2m : 3m
        };
        var summary = Summary(destination, config, options, selected, audit);
        await PersistAndPublishAsync(cart, summary, context, ipAddress, ct, distance);
        return summary;
    }

    private async Task PersistAndPublishAsync(string cart, ShippingSummaryResult summary, RequestContext context,
        string? ipAddress, CancellationToken ct,
        decimal? distance = null, bool publishAdapterRequest = true)
    {
        var cartId = Guid.TryParse(cart, out var parsedCart) ? parsedCart : DeterministicGuid(cart);
        var quotes = summary.ShippingOptions.Select(option =>
        {
            var id = Guid.NewGuid();
            option.ShippingQuoteOptionId = id;
            return new ShippingQuoteRecord
            {
                Id = id,
                TenantId = context.TenantId,
                StoreId = context.StoreId,
                CartId = cartId,
                ProviderCode = option.ShippingModuleCode,
                Option = ToDto(option),
                Delivery = summary.Delivery!,
                Handling = summary.Handling,
                FreeShipping = summary.FreeShipping,
                QuotedAt = DateTimeOffset.UtcNow,
                DistanceKm = distance,
                AppliedRate = distance is null ? null : distance <= 20 ? 2m : 3m
            };
        }).ToList();
        await repository.PersistQuotesAsync(quotes, context, ipAddress, publishAdapterRequest, ct);
        if (publishAdapterRequest)
            foreach (var quote in quotes)
                await events.PublishAdapterExecutionRequestedAsync(quote, context, ct);
    }

    private static ShippingOptionDto ToDto(ShippingOptionResult option) => new()
    {
        OptionPrice = option.OptionPrice,
        OptionPriceText = option.OptionPriceText,
        OptionName = option.OptionName,
        OptionCode = option.OptionCode,
        OptionId = option.OptionId,
        OptionDeliveryDate = option.OptionDeliveryDate?.ToString("O"),
        OptionShippingDate = option.OptionShippingDate?.ToString("O"),
        Description = option.Description,
        ShippingModuleCode = option.ShippingModuleCode,
        Note = option.Note,
        EstimatedNumberOfDays = option.EstimatedNumberOfDays,
        ShippingQuoteOptionId = option.ShippingQuoteOptionId?.ToString()
    };

    private static ShippingSummaryResult Summary(DeliveryAddressDto delivery, ShippingConfigurationRecord config,
        List<ShippingOptionResult> options, ShippingOptionResult? selected, Dictionary<string, object?> audit) =>
        new()
        {
            Shipping = selected?.OptionPrice ?? 0,
            Handling = config.HandlingFees ?? 0,
            ShippingModule = selected?.ShippingModuleCode,
            ShippingOption = selected?.OptionCode,
            FreeShipping = selected?.OptionCode == "FREE_SHIPPING",
            TaxOnShipping = config.TaxOnShipping,
            ShippingQuote = options.Count > 0,
            ShippingText = (selected?.OptionPrice ?? 0).ToString("0.00", CultureInfo.InvariantCulture),
            HandlingText = (config.HandlingFees ?? 0).ToString("0.00", CultureInfo.InvariantCulture),
            Delivery = delivery,
            SelectedShippingOption = selected,
            ShippingOptions = options,
            QuoteInformations = audit
        };

    private static ShippingSummaryResult NoShipping(DeliveryAddressDto delivery, string reason) => new()
    {
        Shipping = 0,
        Handling = 0,
        FreeShipping = false,
        TaxOnShipping = false,
        ShippingQuote = false,
        ShippingText = "0.00",
        HandlingText = "0.00",
        Delivery = delivery,
        QuoteInformations = new Dictionary<string, object?> { ["shippingReturnCode"] = reason }
    };

    private static async Task<ShippingOptionResult> QuoteWithConfiguredAdapterAsync(ShippingModuleRecord provider,
        DeliveryAddressDto destination, List<ShippingPackageFact> facts, decimal? distance, CancellationToken ct)
    {
        if (provider.ModuleCode.Equals("priceByDistance", StringComparison.OrdinalIgnoreCase))
        {
            if (distance is null || string.IsNullOrWhiteSpace(destination.PostalCode))
                throw new DomainException("DISTANCE_UNAVAILABLE",
                    "Distance-based shipping cannot be calculated", 422);
            if (distance > 150) throw new DomainException("DISTANCE_UNAVAILABLE",
                "Distance-based shipping cannot be calculated", 422);
            var rate = distance <= 20 ? 2m : 3m;
            return Option("priceByDistance", "DISTANCE", distance.Value * rate, 0, "Distance shipping");
        }
        if (provider.ModuleCode.Equals("customWeight", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(destination.PostalCode))
                throw new DomainException("DISTANCE_UNAVAILABLE", "A postal code is required", 422);
            if (provider.IntegrationOptions.TryGetValue("regions", out var regions) &&
                regions is JsonElement regionJson && regionJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var region in regionJson.EnumerateArray())
                {
                    var countries = region.TryGetProperty("countries", out var countryJson)
                        ? countryJson.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : [];
                    if (!countries.Contains(destination.CountryCode, StringComparer.OrdinalIgnoreCase)) continue;
                    var regionName = region.TryGetProperty("name", out var regionNameJson)
                        ? regionNameJson.GetString() ?? destination.CountryCode : destination.CountryCode;
                    if (region.TryGetProperty("quoteItems", out var brackets) &&
                        brackets.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var bracket in brackets.EnumerateArray())
                        {
                            var maximum = bracket.GetProperty("maximumWeight").GetDecimal();
                            if (facts.Sum(x => x.Weight) <= maximum)
                                return Option("customWeight", $"CUSTOM_WEIGHT_{regionName}",
                                    bracket.GetProperty("price").GetDecimal(), null, regionName ?? destination.CountryCode ?? "DEFAULT");
                        }
                    }
                }
            }
            throw new DomainException("PROVIDER_ERROR",
                "No configured weight bracket covers this shipment", 502);
        }
        if (provider.ModuleCode.Equals("customQuotesRules", StringComparison.OrdinalIgnoreCase))
        {
            if (distance is null) throw new DomainException("DISTANCE_UNAVAILABLE",
                "Distance-based shipping cannot be calculated", 422);
            if (provider.IntegrationOptions.TryGetValue("distanceRules", out var rules) &&
                rules is JsonElement ruleJson && ruleJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var rule in ruleJson.EnumerateArray())
                {
                    var lower = rule.TryGetProperty("lowerBound", out var low) ? low.GetDecimal() : 0m;
                    var upper = rule.GetProperty("upperBound").GetDecimal();
                    if (distance >= lower && distance <= upper)
                        return Option("customQuotesRules", "customQuotesRules",
                            rule.GetProperty("price").GetDecimal(), null, "Custom shipping");
                }
            }
            throw new DomainException("PROVIDER_ERROR", "No distance rule matches this shipment", 502);
        }
        var price = ParseDecimal(provider.IntegrationKeys.GetValueOrDefault("price")) ??
                    ParseDecimal(provider.IntegrationKeys.GetValueOrDefault("quotePrice"));
        if (price is null)
            throw new DomainException("PROVIDER_ERROR",
                "The configured adapter did not return a normalized quote", 502);
        var days = ParseInt(provider.IntegrationKeys.GetValueOrDefault("estimatedDays"));
        return Option(provider.ModuleCode, provider.ModuleCode.ToUpperInvariant(), price.Value, days,
            provider.IntegrationKeys.GetValueOrDefault("optionName") ?? provider.ModuleCode);
    }

    private static ShippingOptionResult Option(string module, string code, decimal price, int? days, string name) => new()
    {
        OptionPrice = price,
        OptionPriceText = price.ToString("0.00", CultureInfo.InvariantCulture),
        OptionCode = code,
        OptionId = $"{module}_{code}",
        OptionName = name,
        ShippingModuleCode = module,
        EstimatedNumberOfDays = days,
        OptionShippingDate = DateTimeOffset.UtcNow
    };

    private static List<ShippingPackageFact> BuildPackageFacts(ShippingModuleRecord? provider,
        ShippingConfigurationRecord config)
    {
        if (provider?.IntegrationKeys.GetValueOrDefault("productVirtual")
                ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true) return [];
        var weight = ParseDecimal(provider?.IntegrationKeys.GetValueOrDefault("productWeight")) ?? 1m;
        var height = ParseDecimal(provider?.IntegrationKeys.GetValueOrDefault("productHeight")) ?? 4m;
        var length = ParseDecimal(provider?.IntegrationKeys.GetValueOrDefault("productLength")) ?? 4m;
        var width = ParseDecimal(provider?.IntegrationKeys.GetValueOrDefault("productWidth")) ?? 4m;
        var quantity = Math.Max(1, ParseInt(provider?.IntegrationKeys.GetValueOrDefault("quantity")) ?? 1);
        var attributeWeight = ParseDecimal(provider?.IntegrationKeys.GetValueOrDefault("attributeWeight")) ?? 0m;
        weight += attributeWeight;
        var itemFacts = Enumerable.Range(0, quantity).Select(_ => new ShippingPackageFact
        {
            Height = height,
            Length = length,
            Width = width,
            Weight = weight
        }).ToList();
        if (!config.ShippingPackageType.Equals("Box", StringComparison.OrdinalIgnoreCase)) return itemFacts;
        var boxWidth = config.BoxWidth ?? 0; var boxHeight = config.BoxHeight ?? 0; var boxLength = config.BoxLength ?? 0;
        var maxWeight = config.MaxWeight ?? 0; var boxVolume = boxWidth * boxHeight * boxLength;
        if (boxVolume <= 0 || maxWeight <= 0) throw new DomainException("PACKAGE_DOES_NOT_FIT",
            "Product configuration exceeds box configuration", 422);
        var boxes = new List<(decimal VolumeLeft, decimal WeightLeft, decimal Weight)>();
        foreach (var fact in itemFacts)
        {
            var volume = fact.Height * fact.Length * fact.Width;
            if (fact.Width > boxWidth || fact.Height > boxHeight || fact.Length > boxLength ||
                fact.Weight > maxWeight || volume <= 0 || volume > boxVolume)
                throw new DomainException("PACKAGE_DOES_NOT_FIT",
                    "Product dimensions exceed configured box", 422);
            var assigned = false;
            foreach (var box in boxes.ToList())
            {
                if (box.VolumeLeft * .75m >= volume && box.WeightLeft >= fact.Weight)
                {
                    var index = boxes.IndexOf(box);
                    boxes[index] = (box.VolumeLeft - volume, box.WeightLeft - fact.Weight, box.Weight + fact.Weight);
                    assigned = true; break;
                }
            }
            if (!assigned) boxes.Add((boxVolume - volume, maxWeight - fact.Weight, fact.Weight));
        }
        var emptyWeight = config.BoxWeight ?? 0;
        return boxes.Select(box => new ShippingPackageFact
        {
            Height = boxHeight,
            Length = boxLength,
            Width = boxWidth,
            Weight = emptyWeight + box.Weight
        }).ToList();
    }

    private static ShippingOptionResult? SelectOptions(List<ShippingOptionResult> options, string policy)
    {
        if (options.Count == 0) return null;
        var selected = policy.Equals("Highest", StringComparison.OrdinalIgnoreCase)
            ? options.MaxBy(x => x.OptionPrice)
            : options.MinBy(x => x.OptionPrice);
        return selected;
    }

    private static ShippingModuleRecord? SelectProvider(List<ShippingModuleRecord> modules) =>
        modules.FirstOrDefault(x => !IsPreprocessor(x.ModuleCode));

    private static bool IsPreprocessor(string code) =>
        code.Equals("storePickUp", StringComparison.OrdinalIgnoreCase);

    private static void ApplyPreprocessor(ShippingModuleRecord module, DeliveryAddressDto delivery,
        List<ShippingOptionResult> options)
    {
        if (!module.ModuleCode.Equals("storePickUp", StringComparison.OrdinalIgnoreCase)) return;
        var price = ParseDecimal(module.IntegrationKeys.GetValueOrDefault("price")) ??
            throw new DomainException("PICKUP_PRICE_INVALID", "Pickup price must be numeric", 422);
        var region = delivery.ZoneCode ?? delivery.State ?? delivery.CountryCode ?? "DEFAULT";
        options.Add(Option("storePickUp", $"storePickUp_{region}", price, 0, "Store pickup"));
    }

    private static void ValidateModule(string code, string environment)
    {
        if (string.IsNullOrWhiteSpace(code) || !ProviderCodes.Contains(code))
            throw new DomainException("MODULE_NOT_FOUND", "Shipping module is not available", 404);
        if (!environment.Equals("Test", StringComparison.OrdinalIgnoreCase) &&
            !environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("INVALID_REQUEST", "environment must be Test or Production", 400);
    }

    private static void ValidatePackage(ShippingPackageRequestDto request)
    {
        if (!request.Type.Equals("Item", StringComparison.OrdinalIgnoreCase) &&
            !request.Type.Equals("Box", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("PACKAGE_TYPE_INVALID", "type must be Item or Box", 422);
        if (request.ShippingWidth < 0 || request.ShippingHeight < 0 || request.ShippingLength < 0 ||
            request.ShippingWeight < 0 || request.ShippingMaxWeight < 0)
            throw new DomainException("PACKAGE_INVALID", "Package dimensions and weights cannot be negative", 422);
    }

    private static void ValidateCountry(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 2 ||
            value.Any(ch => ch is < 'A' or > 'Z'))
            throw new DomainException("INVALID_REQUEST", $"{field} must be an uppercase ISO-3166 alpha-2 code", 400);
    }

    // @BR-EXT-014: Distance bands are rejected when they overlap without an explicit precedence.
    private static void ValidateDistanceBands(Dictionary<string, object?> options)
    {
        if (!options.TryGetValue("distanceRules", out var raw) || raw is not JsonElement element ||
            element.ValueKind != JsonValueKind.Array) return;
        var bands = element.EnumerateArray().Select(rule =>
        {
            var lower = rule.TryGetProperty("lowerBound", out var low) ? low.GetDecimal() : 0;
            var upper = rule.TryGetProperty("upperBound", out var high) ? high.GetDecimal() : -1;
            var priority = rule.TryGetProperty("priority", out var p) ? p : default;
            return (lower, upper, hasPriority: priority.ValueKind != JsonValueKind.Undefined &&
                priority.ValueKind != JsonValueKind.Null);
        }).ToList();
        if (bands.Any(x => x.lower >= x.upper))
            throw new DomainException("DISTANCE_RULE_OVERLAP", "Distance rule lower bound must be below upper bound", 422);
        for (var i = 0; i < bands.Count; i++)
            for (var j = i + 1; j < bands.Count; j++)
                if (!bands[i].hasPriority && !bands[j].hasPriority &&
                    bands[i].lower < bands[j].upper && bands[j].lower < bands[i].upper)
                    throw new DomainException("DISTANCE_RULE_OVERLAP",
                        "Overlapping distance rules require explicit precedence", 422);
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static Guid DeterministicGuid(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes(value)));

    private static string? DecideProvider(List<ShippingPackageFact> facts, DeliveryAddressDto delivery,
        decimal? distance, List<ShippingModuleRecord> modules)
    {
        var weight = facts.Sum(x => x.Weight);
        var size = facts.Max(x => Math.Max(x.Height, Math.Max(x.Length, x.Width)));
        if (string.Equals(delivery.CountryCode, "CA", StringComparison.OrdinalIgnoreCase) &&
            weight < 62 && size < 66 && modules.Any(x => x.ModuleCode.Equals("canadapost", StringComparison.OrdinalIgnoreCase)))
            return "canadapost";
        if (string.Equals(delivery.CountryCode, "CA", StringComparison.OrdinalIgnoreCase) &&
            (weight > 62 || size > 66) && delivery.ZoneCode?.Equals("QC", StringComparison.OrdinalIgnoreCase) == true &&
            modules.Any(x => x.ModuleCode.Equals("priceByDistance", StringComparison.OrdinalIgnoreCase)))
            return "priceByDistance";
        return null;
    }
}
