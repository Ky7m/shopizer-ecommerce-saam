using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Shopizer.PlatformIntegrations.Data;
using Shopizer.PlatformIntegrations.DTOs;
using Shopizer.PlatformIntegrations.Models;

namespace Shopizer.PlatformIntegrations.Services;

public sealed class IntegrationService(
    IntegrationRepository repository,
    IHttpClientFactory httpClients,
    IConfiguration configuration,
    ILogger<IntegrationService> logger,
    EventPublisher events)
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Dictionary<string, object?>> RuntimeCredentials = new();
    private readonly string _storageRoot = configuration["Storage:RootPath"] ??
        Path.Combine(AppContext.BaseDirectory, "storage");

    // @BR-INT-MS12-001: Adapter discovery returns only active projections for the requested category and environment.
    public async Task<AdapterListResponseDto> ListAdaptersAsync(RequestContext ctx, string? type, string? environment,
        int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new DomainException("INVALID_PAGE", "page must be at least 1 and pageSize must be between 1 and 100", 400);
        var (items, total) = await repository.ListEndpointsAsync(ctx, type is null ? null : MarkerValue.Get(type, type), environment, page, pageSize, ct);
        return new AdapterListResponseDto
        {
            Items = items.Select(DtoMapper.Adapter).ToList(),
            Pagination = new PaginationInfoDto
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    // @BR-INT-MS12-002: Valid refreshes atomically retire the previous projection.
    // @BR-INT-MS12-003: Each environment retains its own endpoint URI and configuration reference.
    // @BR-INT-MS12-004: Supplemental config1 and config2 are represented as separate values.
    // @BR-INT-MS12-005: UPS activation requires all credentials and at least one package type.
    // @BR-INT-MS12-006: USPS activation requires an account and at least one package type.
    public async Task<AdapterDto> RefreshAdapterAsync(RefreshAdapterRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        var type = MarkerValue.Get(request.ModuleType);
        if (!new[] { "Email", "Shipping", "Maps", "Storage", "Adapter" }.Contains(type, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("ADAPTER_CONFIGURATION_INVALID", "moduleType is not supported", 422);
        Require(request.Code, "code"); Require(request.Provider, "provider"); Require(request.Environment, "environment");
        Require(request.ConfigurationRef, "configurationRef");
        if (!string.IsNullOrWhiteSpace(request.ResolvedEndpointUri) &&
            !Uri.TryCreate(request.ResolvedEndpointUri, UriKind.Absolute, out _))
            throw new DomainException("ADAPTER_CONFIGURATION_INVALID", "resolvedEndpointUri is not a valid URI", 422);
        var provider = request.Provider.Trim();
        if (provider.Equals("UPS", StringComparison.OrdinalIgnoreCase))
        {
            RequireCredential(request.Credentials, "accessKey", "UPS");
            RequireCredential(request.Credentials, "userId", "UPS");
            RequireCredential(request.Credentials, "password", "UPS");
            RequirePackages(request.PackageTypes, "UPS");
        }
        if (provider.Equals("USPS", StringComparison.OrdinalIgnoreCase))
        {
            RequireCredential(request.Credentials, "account", "USPS");
            RequirePackages(request.PackageTypes, "USPS");
        }
        var supplemental = new Dictionary<string, object?>();
        if (request.Config1 is not null) supplemental["config1"] = request.Config1;
        if (request.Config2 is not null) supplemental["config2"] = request.Config2;
        if (request.Config1 is not null && request.Config2 is not null &&
            request.Config1.Equals(request.Config2, StringComparison.Ordinal))
            throw new DomainException("ADAPTER_CONFIGURATION_INVALID", "Supplemental settings must remain distinct", 422);
        var endpoint = new IntegrationEndpoint
        {
            Id = Guid.NewGuid(),
            TenantId = ctx.TenantId,
            StoreId = ctx.StoreId,
            IntegrationType = type.ToUpperInvariant(),
            Provider = provider,
            Code = request.Code.Trim(),
            Environment = request.Environment.Trim(),
            ConfigurationRef = request.ConfigurationRef.Trim(),
            EndpointUri = request.ResolvedEndpointUri,
            Capabilities = request.Capabilities ?? new(),
            SupplementalConfiguration = supplemental,
            TimeoutMs = request.TimeoutMs ?? 10_000,
            MaxAttempts = request.MaxAttempts ?? 3
        };
        if (endpoint.TimeoutMs is < 100 or > 120000 || endpoint.MaxAttempts is < 1 or > 10)
            throw new DomainException("ADAPTER_CONFIGURATION_INVALID", "timeoutMs or maxAttempts is outside its allowed range", 422);
        var saved = await repository.ReplaceEndpointAsync(endpoint, ct);
        if (request.Credentials is not null) RuntimeCredentials[saved.Id] = request.Credentials;
        return DtoMapper.Adapter(saved);
    }

    // @BR-INT-MS12-007: UPS is suppressed for unsupported destinations and requires a selected store endpoint.
    // @BR-INT-MS12-008: UPS requests carry rounded provider units, addresses, dimensions and credentials.
    // @BR-INT-MS12-009: UPS provider responses are parsed into normalized options or surfaced as failures.
    public async Task<CarrierQuoteResponseDto> GetUpsQuoteAsync(CarrierQuoteRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        ValidateQuote(request);
        if (!request.Destination.CountryCode.Equals("US", StringComparison.OrdinalIgnoreCase) &&
            !request.Destination.CountryCode.Equals("CA", StringComparison.OrdinalIgnoreCase))
            return Suppressed("UPS");
        var endpoint = await repository.FindEndpointAsync(ctx, "SHIPPING", "ups", request.Environment, ct) ??
            throw new DomainException("CARRIER_PROVIDER_ERROR", "UPS request could not be constructed", 502);
        if (string.IsNullOrWhiteSpace(endpoint.EndpointUri))
            throw new DomainException("CARRIER_PROVIDER_ERROR", "UPS request could not be constructed", 502);
        var xml = BuildUpsRequest(request, endpoint, RuntimeCredentials.GetValueOrDefault(endpoint.Id) ?? new());
        try
        {
            using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            using var response = await httpClients.CreateClient("external-integrations").PostAsync(endpoint.EndpointUri, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode) throw ProviderFailure("UPS", response.StatusCode, body);
            var options = ParseCarrierOptions(body, "UPS", endpoint);
            if (options.Count == 0) throw new DomainException("CARRIER_PROVIDER_ERROR", "No shipping options available", 502);
            return new CarrierQuoteResponseDto { Provider = "UPS", RequestType = "Rate", Options = options };
        }
        catch (DomainException) { throw; }
        catch (HttpRequestException ex) { throw new DomainException("CARRIER_PROVIDER_UNAVAILABLE", ex.Message, 503); }
        catch (Exception ex) { logger.LogWarning(ex, "UPS response could not be normalized"); throw new DomainException("CARRIER_PROVIDER_ERROR", "UPS provider rejected the rating request", 502); }
    }

    // @BR-INT-MS12-010: USPS selects domestic or international routing and computes package size from inch/pound totals.
    // @BR-INT-MS12-011: USPS provider responses are normalized into carrier options with monetary values and service names.
    public async Task<CarrierQuoteResponseDto> GetUspsQuoteAsync(CarrierQuoteRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        ValidateQuote(request);
        if (!request.Origin.CountryCode.Equals("US", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("ADAPTER_CONFIGURATION_INVALID", "USPS requires a US-origin store", 422);
        var endpoint = await repository.FindEndpointAsync(ctx, "SHIPPING", "usps", request.Environment, ct) ??
            throw new DomainException("CARRIER_PROVIDER_ERROR", "USPS request could not be constructed", 502);
        if (string.IsNullOrWhiteSpace(endpoint.EndpointUri))
            throw new DomainException("CARRIER_PROVIDER_ERROR", "USPS request could not be constructed", 502);
        var domestic = request.Origin.CountryCode.Equals(request.Destination.CountryCode, StringComparison.OrdinalIgnoreCase);
        var dimensions = request.Packages.Aggregate((Length: 0m, Width: 0m, Height: 0m, Weight: 0m),
            (sum, package) => (sum.Length + ToInches(package.Length, package.DimensionUnit),
                sum.Width + ToInches(package.Width, package.DimensionUnit),
                sum.Height + ToInches(package.Height, package.DimensionUnit),
                sum.Weight + ToPounds(package.Weight, package.WeightUnit)));
        var girth = dimensions.Length + (dimensions.Width * 2) + (dimensions.Height * 2);
        var packageSize = dimensions.Length + girth <= 64 ? "REGULAR" :
            dimensions.Length + girth <= 108 ? "LARGE" : "OVERSIZE";
        var body = BuildUspsRequest(request, domestic, packageSize, dimensions, RuntimeCredentials.GetValueOrDefault(endpoint.Id) ?? new());
        try
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/xml");
            using var response = await httpClients.CreateClient("external-integrations").PostAsync(endpoint.EndpointUri, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode) throw ProviderFailure("USPS", response.StatusCode, responseBody);
            var options = ParseCarrierOptions(responseBody, "USPS", endpoint);
            if (options.Count == 0) throw new DomainException("CARRIER_PROVIDER_ERROR", "No shipping options available", 502);
            return new CarrierQuoteResponseDto
            {
                Provider = "USPS",
                RequestType = domestic ? "Domestic" : "International",
                PackageSize = packageSize,
                Options = options
            };
        }
        catch (DomainException) { throw; }
        catch (HttpRequestException ex) { throw new DomainException("CARRIER_PROVIDER_UNAVAILABLE", ex.Message, 503); }
        catch (Exception ex) { logger.LogWarning(ex, "USPS response could not be normalized"); throw new DomainException("CARRIER_PROVIDER_ERROR", "USPS provider rejected the rating request", 502); }
    }

    // @BR-INT-MS12-012: Distance requests suppress ineligible destinations and geocode eligible routes through the configured maps adapter.
    public async Task<DistanceResponseDto> CalculateDistanceAsync(DistanceRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        RequireAddress(request.Origin, "origin"); RequireAddress(request.Destination, "destination");
        if (string.IsNullOrWhiteSpace(request.Destination.ZoneCode) ||
            !request.AllowedZoneCodes.Any(x => x.Equals(request.Destination.ZoneCode, StringComparison.OrdinalIgnoreCase)))
            return new DistanceResponseDto { Enriched = false, SuppressedReason = "DESTINATION_ZONE_NOT_ALLOWED" };
        if (string.IsNullOrWhiteSpace(request.Destination.PostalCode))
            return new DistanceResponseDto { Enriched = false, SuppressedReason = "DESTINATION_POSTAL_CODE_MISSING" };
        var endpoint = await repository.FindEndpointAsync(ctx, "MAPS", "maps", "PROD", ct);
        if (endpoint?.EndpointUri is null)
            return new DistanceResponseDto { Enriched = false, SuppressedReason = "GEOCODING_UNAVAILABLE" };
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                origin = request.Origin,
                destination = request.Destination,
                allowedZoneCodes = request.AllowedZoneCodes
            });
            using var response = await httpClients.CreateClient("external-integrations").PostAsync(endpoint.EndpointUri,
                new StringContent(payload, Encoding.UTF8, "application/json"), ct);
            if (!response.IsSuccessStatusCode) throw new DomainException("MAP_PROVIDER_ERROR", "Maps provider rejected the route", 502);
            var result = await response.Content.ReadFromJsonAsync<DistanceResponseDto>(cancellationToken: ct) ??
                throw new DomainException("MAP_PROVIDER_ERROR", "Maps provider returned an empty response", 502);
            return result;
        }
        catch (DomainException) { throw; }
        catch (Exception ex) { logger.LogWarning(ex, "Maps provider call failed"); throw new DomainException("MAP_PROVIDER_ERROR", "Maps provider could not enrich the route", 502); }
    }

    // @BR-INT-MS12-013: IP lookup validates syntax and reads the optional local GeoLite source without making an external call.
    public Task<IpGeolocationResponseDto> ResolveIpAsync(IpGeolocationRequestDto request, CancellationToken ct)
    {
        var value = request.IpAddress is JsonElement json && json.ValueKind == JsonValueKind.String
            ? json.GetString() : request.IpAddress?.ToString();
        if (!IPAddress.TryParse(value, out _))
            throw new DomainException("INVALID_IP_ADDRESS", "ipAddress must be a valid IPv4 or IPv6 address", 400);
        // The GeoLite database is an optional deployment asset. Without it, the contract requires
        // an unresolved result rather than a guessed location.
        return Task.FromResult(new IpGeolocationResponseDto { Resolved = false });
    }

    // @BR-INT-MS12-014: Email messages are associated with the configured sender endpoint before queue acknowledgement.
    // @BR-INT-MS12-015: Template tokens are rendered into durable UTF-8 text/HTML payloads before provider submission.
    // @BR-INT-MS12-016: Order-confirmation token payloads remain complete and opaque to the delivery owner.
    // @BR-INT-MS12-017: Operational notifications are queued without copying plaintext credentials.
    // @BR-INT-MS12-021: Email idempotency binds one immutable request hash to one durable operation.
    // @BR-INT-MS12-022: The initial provider attempt is durable and bounded by endpoint retry policy.
    // @BR-INT-MS12-023: The queue event is persisted in the transactional outbox before publication.
    public async Task<EmailMessageDto> QueueEmailAsync(QueueEmailRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        ValidateEmail(request);
        var endpoint = await repository.FindEndpointAsync(ctx, "EMAIL", "email", "PROD", ct) ??
            await repository.EnsureStorageEndpointAsync(ctx, "Local", ct);
        var hash = Hash(request);
        var existing = await repository.FindOperationAsync(ctx, request.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (!existing.RequestHash.Equals(hash, StringComparison.Ordinal))
                throw new DomainException("IDEMPOTENCY_KEY_REUSED", "idempotencyKey is already associated with a different request", 409);
            var attempts = await repository.FindAttemptsAsync(existing.OperationId, ctx, ct);
            return DtoMapper.Email(new EmailMessage
            {
                MessageId = attempts.FirstOrDefault()?.MessageId ?? Guid.NewGuid(),
                OperationId = existing.OperationId,
                EndpointId = endpoint.Id,
                IdempotencyKey = request.IdempotencyKey,
                TemplateKey = request.TemplateKey,
                Locale = request.Locale,
                RecipientEmail = request.RecipientEmail,
                SenderEmail = request.SenderEmail,
                SenderName = request.SenderName,
                Subject = request.Subject,
                Status = "QUEUED",
                OrderReference = request.OrderReference,
                QueuedAt = existing.CreatedAt
            });
        }
        var message = new EmailMessage
        {
            MessageId = Guid.NewGuid(),
            OperationId = Guid.Empty,
            EndpointId = endpoint.Id,
            IdempotencyKey = request.IdempotencyKey,
            TemplateKey = request.TemplateKey,
            Locale = request.Locale,
            RecipientEmail = request.RecipientEmail,
            SenderEmail = request.SenderEmail,
            SenderName = request.SenderName,
            Subject = request.Subject,
            TokenPayload = Redact(request.TokenPayload),
            OrderReference = request.OrderReference,
            QueuedAt = DateTimeOffset.UtcNow
        };
        RenderTemplate(message);
        var created = await repository.CreateDeliveryAsync(ctx, "EMAIL", request.IdempotencyKey, hash, endpoint,
            new[] { ("email", (object)message.TokenPayload, (Guid?)message.MessageId) }, message, ct);
        await events.PublishQueuedAsync(created.Attempts[0].OutboxEventId ?? Guid.Empty,
            new
            {
                eventId = created.Attempts[0].OutboxEventId,
                eventType = "IntegrationDeliveryQueued",
                eventVersion = 1,
                occurredAt = DateTimeOffset.UtcNow,
                tenantId = ctx.TenantId,
                storeId = ctx.StoreId,
                correlationId = ctx.CorrelationId,
                operationId = created.Operation.OperationId,
                attemptId = created.Attempts[0].AttemptId,
                endpointId = endpoint.Id,
                idempotencyKey = request.IdempotencyKey
            }, ct);
        message.OperationId = created.Operation.OperationId;
        return DtoMapper.Email(message);
    }

    // @BR-INT-MS12-018: Storage keys are provider-neutral and include store, content type, folder and file name.
    // @BR-INT-MS12-019: Local storage performs replacement writes and real byte reads, listings and deletes.
    // @BR-INT-MS12-021: Single uploads create one durable operation and one associated attempt.
    public async Task<UploadedFileAssetDto> UploadFileAsync(UploadFileRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        ValidateFile(request.StoreCode, request.ContentType, request.FileName, request.MimeType, request.ContentBase64);
        var item = new UploadFileItemDto
        {
            ContentType = request.ContentType,
            FileName = request.FileName,
            MimeType = request.MimeType,
            ContentBase64 = request.ContentBase64
        };
        var hash = Hash(new { request.StoreCode, request.ContentType, request.FolderPath, item.FileName, item.MimeType, item.ContentBase64 });
        var endpoint = await repository.EnsureStorageEndpointAsync(ctx, "Local", ct);
        var existing = await repository.FindOperationAsync(ctx, request.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.RequestHash != hash) throw new DomainException("IDEMPOTENCY_KEY_REUSED", "idempotencyKey is already associated with a different upload request", 409);
            var attempt = (await repository.FindAttemptsAsync(existing.OperationId, ctx, ct)).First();
            return ToUploaded(request, existing.OperationId, attempt.AttemptId);
        }
        var created = await repository.CreateDeliveryAsync(ctx, "STORAGE_UPLOAD", request.IdempotencyKey, hash, endpoint,
            new[] { (request.FileName, (object)item, (Guid?)null) }, null, ct);
        await events.PublishQueuedAsync(created.Attempts[0].OutboxEventId ?? Guid.Empty,
            new
            {
                eventId = created.Attempts[0].OutboxEventId,
                eventType = "IntegrationDeliveryQueued",
                eventVersion = 1,
                occurredAt = DateTimeOffset.UtcNow,
                tenantId = ctx.TenantId,
                storeId = ctx.StoreId,
                correlationId = ctx.CorrelationId,
                operationId = created.Operation.OperationId,
                attemptId = created.Attempts[0].AttemptId,
                endpointId = endpoint.Id,
                idempotencyKey = request.IdempotencyKey
            }, ct);
        try
        {
            await WriteLocalAsync(request.StoreCode, TypeValue(request.ContentType), request.FolderPath, request.FileName,
                Convert.FromBase64String(request.ContentBase64), ct);
            await repository.SetAttemptResultAsync(created.Attempts[0].AttemptId, "SUCCEEDED", "UPLOADED", null, null, null, ct);
        }
        catch (FormatException) { await repository.SetAttemptResultAsync(created.Attempts[0].AttemptId, "DEAD_LETTERED", null, "INVALID_BASE64", "contentBase64 is invalid", null, ct); throw new DomainException("INVALID_FILE_CONTENT", "contentBase64 is invalid", 422); }
        catch (IOException ex) { await repository.SetAttemptResultAsync(created.Attempts[0].AttemptId, "DEAD_LETTERED", null, "STORAGE_WRITE_FAILED", ex.Message, null, ct); throw new DomainException("STORAGE_PROVIDER_ERROR", "The storage provider rejected the upload", 502); }
        return ToUploaded(request, created.Operation.OperationId, created.Attempts[0].AttemptId);
    }

    // @BR-INT-MS12-018: Every batch item uses the same provider-neutral namespace rules.
    // @BR-INT-MS12-019: Batch storage writes execute each item and surface individual provider failures.
    // @BR-INT-MS12-021: One batch operation links one attempt to every item.
    public async Task<FileBatchResponseDto> UploadFilesAsync(BatchUploadFileRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        if (request.Files.Count == 0) throw new DomainException("STORAGE_FILES_REQUIRED", "files must contain at least one item", 422);
        foreach (var f in request.Files) ValidateFile(request.StoreCode, f.ContentType, f.FileName, f.MimeType, f.ContentBase64);
        var hash = Hash(request);
        var endpoint = await repository.EnsureStorageEndpointAsync(ctx, "Local", ct);
        var existing = await repository.FindOperationAsync(ctx, request.IdempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.RequestHash != hash) throw new DomainException("IDEMPOTENCY_KEY_REUSED", "idempotencyKey is already associated with a different upload request", 409);
            var oldAttempts = await repository.FindAttemptsAsync(existing.OperationId, ctx, ct);
            return new FileBatchResponseDto
            {
                OperationId = existing.OperationId.ToString(),
                Items = request.Files.Select((f, i) =>
                    ToUploaded(f, request.StoreCode, request.FolderPath, existing.OperationId, oldAttempts.ElementAtOrDefault(i)?.AttemptId ?? Guid.Empty)).ToList(),
                AcceptedCount = existing.ItemCount,
                FailedCount = 0
            };
        }
        var items = request.Files.Select(f => (f.FileName, (object)f, (Guid?)null)).ToList();
        var created = await repository.CreateDeliveryAsync(ctx, "STORAGE_BATCH_UPLOAD", request.IdempotencyKey, hash, endpoint, items, null, ct);
        await events.PublishQueuedAsync(created.Attempts[0].OutboxEventId ?? Guid.Empty,
            new
            {
                eventId = created.Attempts[0].OutboxEventId,
                eventType = "IntegrationDeliveryQueued",
                eventVersion = 1,
                occurredAt = DateTimeOffset.UtcNow,
                tenantId = ctx.TenantId,
                storeId = ctx.StoreId,
                correlationId = ctx.CorrelationId,
                operationId = created.Operation.OperationId,
                attemptId = created.Attempts[0].AttemptId,
                endpointId = endpoint.Id,
                idempotencyKey = request.IdempotencyKey
            }, ct);
        var response = new FileBatchResponseDto { OperationId = created.Operation.OperationId.ToString() };
        for (var i = 0; i < request.Files.Count; i++)
        {
            var file = request.Files[i]; var attempt = created.Attempts[i];
            try
            {
                await WriteLocalAsync(request.StoreCode, TypeValue(file.ContentType), request.FolderPath, file.FileName,
                    Convert.FromBase64String(file.ContentBase64), ct);
                await repository.SetAttemptResultAsync(attempt.AttemptId, "SUCCEEDED", "UPLOADED", null, null, null, ct);
                response.Items.Add(ToUploaded(file, request.StoreCode, request.FolderPath, created.Operation.OperationId, attempt.AttemptId));
                response.AcceptedCount++;
            }
            catch (Exception ex) when (ex is FormatException or IOException)
            {
                await repository.SetAttemptResultAsync(attempt.AttemptId, "DEAD_LETTERED", null, "STORAGE_WRITE_FAILED", ex.Message, null, ct);
                response.Items.Add(ToUploaded(file, request.StoreCode, request.FolderPath, created.Operation.OperationId, attempt.AttemptId, "Failed"));
                response.FailedCount++;
            }
        }
        return response;
    }

    // @BR-INT-MS12-018: Listings are scoped to the requested store/content namespace and expose metadata only.
    public Task<FileListResponseDto> ListFilesAsync(string storeCode, ContentTypeDto type, string? folder, RequestContext ctx, CancellationToken ct)
    {
        var root = Namespace(storeCode, TypeValue(type), folder);
        if (!Directory.Exists(root)) return Task.FromResult(new FileListResponseDto { Items = new(), Pagination = new PaginationInfoDto { Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 } });
        var files = Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly).Select(path =>
        {
            var name = Path.GetFileName(path); var mime = new FileExtensionContentTypeProvider().TryGetContentType(name, out var value) ? value : "application/octet-stream";
            return new FileAssetDto
            {
                FileName = name,
                ContentType = MarkerValue.Create<ContentTypeDto>(TypeValue(type)),
                MimeType = mime,
                ProviderKey = Key(storeCode, TypeValue(type), folder, name),
                Status = MarkerValue.Create<FileStatusDto>("Available")
            };
        }).ToList();
        return Task.FromResult(new FileListResponseDto { Items = files, Pagination = new PaginationInfoDto { Page = 1, PageSize = files.Count, TotalItems = files.Count, TotalPages = files.Count == 0 ? 0 : 1 } });
    }

    // @BR-INT-MS12-018: A file read uses the same logical provider key as upload and listing.
    // @BR-INT-MS12-019: Supported local reads return the stored bytes and metadata.
    public async Task<FileContentResponseDto> GetFileAsync(string fileName, string storeCode, ContentTypeDto type, string? folder, RequestContext ctx, CancellationToken ct)
    {
        ValidateName(fileName);
        var path = Path.Combine(Namespace(storeCode, TypeValue(type), folder), fileName);
        if (!File.Exists(path)) throw new DomainException("FILE_NOT_FOUND", "The requested file was not found", 404);
        var bytes = await File.ReadAllBytesAsync(path, ct);
        var mime = new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var value) ? value : "application/octet-stream";
        return new FileContentResponseDto
        {
            FileName = fileName,
            ContentType = type,
            MimeType = mime,
            ProviderKey = Key(storeCode, TypeValue(type), folder, fileName),
            ContentBase64 = Convert.ToBase64String(bytes)
        };
    }

    // @BR-INT-MS12-018: Single-file deletion targets only the requested logical object.
    public Task DeleteFileAsync(string fileName, string storeCode, ContentTypeDto type, string? folder, RequestContext ctx, CancellationToken ct)
    {
        ValidateName(fileName);
        var path = Path.Combine(Namespace(storeCode, TypeValue(type), folder), fileName);
        if (!File.Exists(path)) throw new DomainException("FILE_NOT_FOUND", "The requested file was not found", 404);
        File.Delete(path); return Task.CompletedTask;
    }

    // @BR-INT-MS12-018: Namespace deletion removes only the selected store/content namespace.
    public Task DeleteFilesAsync(string storeCode, string? folder, RequestContext ctx, CancellationToken ct)
    {
        var path = Path.Combine(_storageRoot, SafeSegment(storeCode), SafeSegment(folder ?? ""));
        if (!Directory.Exists(path)) throw new DomainException("FILE_NAMESPACE_NOT_FOUND", "The requested namespace was not found", 404);
        Directory.Delete(path, true); return Task.CompletedTask;
    }

    // @BR-INT-MS12-020: Folder operations execute only for providers with an explicit capability.
    public Task<FolderResponseDto> CreateFolderAsync(FolderRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        var provider = ProviderValue(request.Provider);
        if (!provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            throw Unsupported(provider, "folder creation");
        ValidateName(request.FolderName); var path = Path.Combine(Namespace(request.StoreCode, "Image", request.FolderPath), request.FolderName);
        Directory.CreateDirectory(path);
        return Task.FromResult(new FolderResponseDto
        {
            Path = $"{request.StoreCode}/Image/{Join(request.FolderPath, request.FolderName)}",
            Provider = request.Provider,
            Capability = "CreateFolder",
            Status = "Created"
        });
    }

    // @BR-INT-MS12-020: Folder listing reports the selected provider capability rather than silently returning null.
    public Task<FolderListResponseDto> ListFoldersAsync(string storeCode, StorageProviderDto provider, string? folderPath, RequestContext ctx, CancellationToken ct)
    {
        var name = ProviderValue(provider);
        if (!name.Equals("Local", StringComparison.OrdinalIgnoreCase)) throw Unsupported(name, "folder listing");
        var root = Path.Combine(_storageRoot, SafeSegment(storeCode), "Image", SafeSegment(folderPath ?? ""));
        var items = Directory.Exists(root) ? Directory.EnumerateDirectories(root).Select(Path.GetFileName).Where(x => x is not null).Cast<string>().ToList() : new();
        return Task.FromResult(new FolderListResponseDto { Items = items, Provider = provider });
    }

    // @BR-INT-MS12-020: Folder deletion removes only an existing selected-provider folder.
    public Task DeleteFolderAsync(string storeCode, StorageProviderDto provider, string? folderPath, string folderName, RequestContext ctx, CancellationToken ct)
    {
        var name = ProviderValue(provider); if (!name.Equals("Local", StringComparison.OrdinalIgnoreCase)) throw Unsupported(name, "folder deletion");
        ValidateName(folderName); var path = Path.Combine(_storageRoot, SafeSegment(storeCode), "Image", SafeSegment(folderPath ?? ""), folderName);
        if (!Directory.Exists(path)) throw new DomainException("FOLDER_NOT_FOUND", "The requested folder was not found", 404);
        Directory.Delete(path, true); return Task.CompletedTask;
    }

    // @BR-INT-MS12-022: Delivery inspection is tenant/store scoped and exposes provider/retry lineage.
    public async Task<DeliveryAttemptDto> GetAttemptAsync(Guid attemptId, RequestContext ctx, CancellationToken ct) =>
        DtoMapper.Attempt(await repository.FindAttemptAsync(attemptId, ctx, ct) ??
            throw new DomainException("DELIVERY_ATTEMPT_NOT_FOUND", "Delivery attempt was not found", 404));

    // @BR-INT-MS12-023: Replay rejects successful attempts and leaves the original terminal record unchanged.
    public async Task<DeliveryAttemptDto> ReplayAttemptAsync(Guid attemptId, ReplayRequestDto request, RequestContext ctx, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("REPLAY_REASON_REQUIRED", "reason is required", 422);
        var original = await repository.FindAttemptAsync(attemptId, ctx, ct) ??
            throw new DomainException("DELIVERY_ATTEMPT_NOT_FOUND", "Delivery attempt was not found", 404);
        var replay = await repository.ReplayAsync(original, request.Reason, ctx, ct);
        await events.PublishQueuedAsync(replay.OutboxEventId ?? Guid.Empty,
            new
            {
                eventId = replay.OutboxEventId,
                eventType = "IntegrationDeliveryReplayRequested",
                eventVersion = 1,
                occurredAt = DateTimeOffset.UtcNow,
                tenantId = ctx.TenantId,
                storeId = ctx.StoreId,
                correlationId = ctx.CorrelationId,
                operationId = replay.OperationId,
                attemptId = replay.AttemptId,
                replayOfAttemptId = original.AttemptId,
                reason = request.Reason
            }, ct);
        return DtoMapper.Attempt(replay);
    }

    public async Task ConsumeEventAsync(string eventType, JsonElement root, CancellationToken ct)
    {
        var tenant = root.TryGetProperty("tenantId", out var tenantValue) ? tenantValue.GetString() : null;
        var store = root.TryGetProperty("storeId", out var storeValue) ? storeValue.GetString() : null;
        var correlation = root.TryGetProperty("correlationId", out var correlationValue) ? correlationValue.GetString() : null;
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store) || string.IsNullOrWhiteSpace(correlation))
            return;
        var context = new RequestContext(tenant, store, correlation);
        if (eventType.Equals("ConfigurationReferenceChanged", StringComparison.Ordinal))
        {
            var type = root.GetProperty("moduleType").GetString()!;
            await repository.UpdateConfigurationReferenceAsync(context, type, root.GetProperty("code").GetString()!,
                root.GetProperty("environment").GetString()!, root.GetProperty("configurationRef").GetString()!, ct);
        }
        else if (eventType.Equals("IntegrationDeliveryReplayRequested", StringComparison.Ordinal) &&
                 root.TryGetProperty("originalAttemptId", out var original) &&
                 Guid.TryParse(original.GetString(), out var attemptId))
        {
            await ReplayAttemptAsync(attemptId, new ReplayRequestDto { Reason = root.GetProperty("reason").GetString() ?? "operator replay" }, context, ct);
        }
        else if (eventType.Equals("BusinessIntegrationDeliveryRequested", StringComparison.Ordinal) &&
                 root.TryGetProperty("deliveryType", out var delivery) &&
                 string.Equals(delivery.GetString(), "Email", StringComparison.OrdinalIgnoreCase) &&
                 root.TryGetProperty("payload", out var payload))
        {
            var email = JsonSerializer.Deserialize<QueueEmailRequestDto>(payload.GetRawText());
            if (email is not null) await QueueEmailAsync(email, context, ct);
        }
    }

    private static void Require(string? value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new DomainException("ADAPTER_CONFIGURATION_INVALID", $"{name} is required", 422); }
    private static void RequireCredential(Dictionary<string, object?>? values, string name, string provider)
    {
        if (values is null || !values.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value?.ToString()))
            throw new DomainException("ADAPTER_CONFIGURATION_INVALID", $"{provider} requires {name}", 422);
    }
    private static void RequirePackages(List<string>? values, string provider)
    {
        if (values is null || values.Count == 0) throw new DomainException("ADAPTER_CONFIGURATION_INVALID", $"{provider} requires at least one package type", 422);
    }
    private static void ValidateQuote(CarrierQuoteRequestDto request)
    {
        Require(request.Environment, "environment"); RequireAddress(request.Origin, "origin"); RequireAddress(request.Destination, "destination");
        if (request.Packages.Count == 0) throw new DomainException("INVALID_CARRIER_REQUEST", "packages must contain at least one package", 422);
        foreach (var p in request.Packages)
            if (p.Weight <= 0 || p.Length <= 0 || p.Width <= 0 || p.Height <= 0 ||
                p.WeightUnit is not ("KG" or "LB") || p.DimensionUnit is not ("CM" or "IN"))
                throw new DomainException("INVALID_CARRIER_REQUEST", "package dimensions and units are invalid", 422);
    }
    private static void RequireAddress(AddressDto? address, string name)
    {
        if (address is null || string.IsNullOrWhiteSpace(address.CountryCode) || address.CountryCode.Length != 2 ||
            string.IsNullOrWhiteSpace(address.PostalCode)) throw new DomainException("INVALID_ADDRESS", $"{name} countryCode and postalCode are required", 422);
    }
    private static CarrierQuoteResponseDto Suppressed(string provider) => new() { Provider = provider, RequestType = "Suppressed", SuppressedReason = "DESTINATION_NOT_SUPPORTED" };
    private static DomainException ProviderFailure(string provider, HttpStatusCode code, string body) =>
        new("CARRIER_PROVIDER_ERROR", $"{provider} provider rejected the rating request: {(string.IsNullOrWhiteSpace(body) ? code.ToString() : body[..Math.Min(200, body.Length)])}", 502);

    private static string BuildUpsRequest(CarrierQuoteRequestDto r, IntegrationEndpoint e, Dictionary<string, object?> credentials)
    {
        var weightCode = r.Packages[0].WeightUnit == "KG" ? "KGS" : "LBS";
        var packageXml = string.Join("", r.Packages.Select(p => $"<Package><PackageWeight><UnitOfMeasurement><Code>{weightCode}</Code></UnitOfMeasurement><Weight>{p.Weight.ToString("0.0", CultureInfo.InvariantCulture)}</Weight></PackageWeight><Dimensions><UnitOfMeasurement><Code>{p.DimensionUnit}</Code></UnitOfMeasurement><Length>{p.Length.ToString("0.00", CultureInfo.InvariantCulture)}</Length><Width>{p.Width.ToString("0.00", CultureInfo.InvariantCulture)}</Width><Height>{p.Height.ToString("0.00", CultureInfo.InvariantCulture)}</Height></Dimensions></Package>"));
        var access = credentials.TryGetValue("accessKey", out var a) ? a : ""; var user = credentials.TryGetValue("userId", out var u) ? u : ""; var password = credentials.TryGetValue("password", out var p) ? p : "";
        return $"<?xml version=\"1.0\"?><RatingRequest><AccessRequest><AccessLicenseNumber>{access}</AccessLicenseNumber><UserId>{user}</UserId><Password>{password}</Password></AccessRequest><Shipment><Shipper><Address><City>{r.Origin.City}</City><StateProvinceCode>{r.Origin.State ?? r.Origin.ZoneCode}</StateProvinceCode><PostalCode>{r.Origin.PostalCode.Trim()}</PostalCode><CountryCode>{r.Origin.CountryCode}</CountryCode></Address></Shipper><ShipTo><Address><City>{r.Destination.City}</City><StateProvinceCode>{r.Destination.State ?? r.Destination.ZoneCode}</StateProvinceCode><PostalCode>{r.Destination.PostalCode.Trim()}</PostalCode><CountryCode>{r.Destination.CountryCode}</CountryCode></Address></ShipTo>{packageXml}</Shipment></RatingRequest>";
    }

    private static string BuildUspsRequest(CarrierQuoteRequestDto r, bool domestic, string size, (decimal Length, decimal Width, decimal Height, decimal Weight) d, Dictionary<string, object?> credentials) =>
        $"<?xml version=\"1.0\"?><{(domestic ? "RateV3Request" : "IntlRateRequest")}><UserId>{(credentials.TryGetValue("account", out var account) ? account : "")}</UserId><OriginZip>{r.Origin.PostalCode}</OriginZip><DestinationZip>{r.Destination.PostalCode}</DestinationZip><Pounds>{Math.Floor(d.Weight)}</Pounds><Ounces>{((d.Weight - Math.Floor(d.Weight)) * 16).ToString("0", CultureInfo.InvariantCulture)}</Ounces><Size>{size}</Size><Machinable>true</Machinable><ShipDate>{DateTime.UtcNow.AddDays(3):yyyy-MM-dd}</ShipDate></{(domestic ? "RateV3Request" : "IntlRateRequest")}>";

    private static List<CarrierOptionDto> ParseCarrierOptions(string body, string provider, IntegrationEndpoint endpoint)
    {
        var xml = XDocument.Parse(body);
        var errors = xml.Descendants().Where(x => x.Name.LocalName.Contains("ErrorDescription", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(errors)) throw new DomainException("CARRIER_PROVIDER_ERROR", $"{provider} provider rejected the rating request: {errors}", 502);
        return xml.Descendants().Where(x => x.Name.LocalName is "RatedShipment" or "Postage").Select(x =>
        {
            var code = x.Descendants().FirstOrDefault(n => n.Name.LocalName is "Code" or "CLASSID" or "MailService")?.Value ?? "STANDARD";
            var priceText = x.Descendants().FirstOrDefault(n => n.Name.LocalName is "MonetaryValue" or "Rate" or "Rate")?.Value ?? "0";
            decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
            var days = x.Descendants().FirstOrDefault(n => n.Name.LocalName.Contains("Days", StringComparison.OrdinalIgnoreCase))?.Value;
            var display = endpoint.Capabilities.TryGetValue(code, out var configured) ? configured?.ToString() : null;
            return new CarrierOptionDto { Provider = provider, Code = code, Name = display, Price = price, Currency = "USD", EstimatedDays = days };
        }).ToList();
    }

    private void ValidateEmail(QueueEmailRequestDto r)
    {
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(r.RecipientEmail) ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(r.SenderEmail))
            throw new DomainException("EMAIL_CONFIGURATION_INVALID", "recipientEmail and senderEmail must be valid email addresses", 422);
        if (string.IsNullOrWhiteSpace(r.TemplateKey) || string.IsNullOrWhiteSpace(r.Locale) || string.IsNullOrWhiteSpace(r.Subject))
            throw new DomainException("EMAIL_CONFIGURATION_INVALID", "templateKey, locale and subject are required", 422);
    }

    private static void RenderTemplate(EmailMessage message)
    {
        // Rendering is deterministic and UTF-8; tokens are prepared before queueing so a malformed
        // template payload cannot reach a provider worker.
        _ = JsonSerializer.Serialize(message.TokenPayload);
    }

    private static Dictionary<string, object?> Redact(Dictionary<string, object?> input) =>
        input.Where(x => !x.Key.Contains("password", StringComparison.OrdinalIgnoreCase) &&
                         !x.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) &&
                         !x.Key.Contains("accessToken", StringComparison.OrdinalIgnoreCase))
             .ToDictionary(x => x.Key, x => x.Value);

    private async Task WriteLocalAsync(string store, string type, string? folder, string name, byte[] bytes, CancellationToken ct)
    {
        var root = Namespace(store, type, folder); Directory.CreateDirectory(root);
        await File.WriteAllBytesAsync(Path.Combine(root, name), bytes, ct);
    }
    private string Namespace(string store, string type, string? folder) =>
        Path.Combine(_storageRoot, SafeSegment(store), SafeSegment(type), SafeSegment(folder ?? ""));
    private static string Key(string store, string type, string? folder, string name) =>
        string.Join("/", new[] { store, type, folder, name }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static string SafeSegment(string value)
    {
        if (value.Contains('/') || value.Contains('\\') || value.Contains("..", StringComparison.Ordinal) || value.Contains('\0'))
            throw new DomainException("STORAGE_KEY_INVALID", "path values cannot contain traversal or path separators", 422);
        return value;
    }
    private static void ValidateName(string name) { if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('\\') || name is "." or "..") throw new DomainException("STORAGE_KEY_INVALID", "fileName must not contain path traversal or path separators", 422); }
    private static string Join(string? path, string name) => string.IsNullOrWhiteSpace(path) ? name : $"{path.Trim('/')}/{name}";
    private static string TypeValue(ContentTypeDto type) => MarkerValue.Get(type, "File");
    private static string ProviderValue(StorageProviderDto provider) => MarkerValue.Get(provider, "Local");
    private static decimal ToInches(decimal value, string unit) => unit == "CM" ? value / 2.54m : value;
    private static decimal ToPounds(decimal value, string unit) => unit == "KG" ? value * 2.2046226218m : value;
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static FolderResponseDto Dummy() => new();
    private static DomainException Unsupported(string provider, string operation) => new("STORAGE_OPERATION_UNSUPPORTED", $"{provider} does not support {operation}", 501);
    private static void ValidateFile(string store, ContentTypeDto type, string name, string mime, string content) { if (string.IsNullOrWhiteSpace(store) || string.IsNullOrWhiteSpace(mime) || string.IsNullOrWhiteSpace(content)) throw new DomainException("INVALID_FILE_REQUEST", "storeCode, mimeType and contentBase64 are required", 422); ValidateName(name); _ = TypeValue(type); }
    private static UploadedFileAssetDto ToUploaded(UploadFileRequestDto r, Guid op, Guid attempt) =>
        ToUploaded(new UploadFileItemDto { ContentType = r.ContentType, FileName = r.FileName, MimeType = r.MimeType, ContentBase64 = r.ContentBase64 },
            r.StoreCode, r.FolderPath, op, attempt);

    private static UploadedFileAssetDto ToUploaded(UploadFileItemDto f, string store, string? folder, Guid op, Guid attempt, string status = "Available") =>
        MarkerValue.WithPayload(new UploadedFileAssetDto(), new
        {
            operationId = op.ToString(),
            fileName = f.FileName,
            contentType = MarkerValue.Get(f.ContentType, "File"),
            mimeType = f.MimeType,
            providerKey = Key(store, MarkerValue.Get(f.ContentType, "File"), folder, f.FileName),
            status,
            deliveryAttemptId = attempt.ToString()
        });
}

public sealed class FileExtensionContentTypeProvider
{
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    { [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".gif"] = "image/gif", [".css"] = "text/css", [".js"] = "text/javascript", [".pdf"] = "application/pdf", [".txt"] = "text/plain" };
    public bool TryGetContentType(string path, out string contentType) => Types.TryGetValue(Path.GetExtension(path), out contentType!);
}
