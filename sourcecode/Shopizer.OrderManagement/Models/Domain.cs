using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shopizer.OrderManagement.DTOs;

namespace Shopizer.OrderManagement.Models;

public sealed record RequestContext(string TenantId, string StoreId, string CorrelationId)
{
    public long StoreNumber => StoreId switch
    {
        _ when long.TryParse(StoreId, out var number) => number,
        _ when StoreId.StartsWith("store-", StringComparison.OrdinalIgnoreCase) &&
               long.TryParse(StoreId[6..], out var suffix) => suffix,
        _ => StableNumber(StoreId)
    };

    public static RequestContext From(HttpContext http)
    {
        var tenant = http.Request.Headers["x-tenant-id"].FirstOrDefault();
        var store = http.Request.Headers["x-store-id"].FirstOrDefault();
        var correlation = http.Request.Headers["x-correlation-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(store) ||
            string.IsNullOrWhiteSpace(correlation))
            throw new DomainException("REQUEST_CONTEXT_REQUIRED", "x-tenant-id, x-store-id and x-correlation-id are required", 400);
        return new(tenant.Trim(), store.Trim(), correlation.Trim());
    }

    private static long StableNumber(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        var number = BitConverter.ToInt64(hash, 0) & long.MaxValue;
        return number == 0 ? 1 : number;
    }
}

public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed class Order
{
    public long OrderId { get; init; }
    public string TenantId { get; init; } = "";
    public long StoreId { get; init; }
    public long? CustomerId { get; set; }
    public string? CustomerEmailAddress { get; set; }
    public string Status { get; set; } = "ORDERED";
    public string PaymentStatus { get; set; } = "PENDING";
    public string FulfillmentStatus { get; set; } = "NOT_REQUESTED";
    public string CurrencyCode { get; set; } = "";
    public decimal Total { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal RefundableAmount { get; set; }
    public DateTimeOffset DatePurchased { get; init; }
    public DateTimeOffset? OrderDateFinished { get; set; }
    public string? PaymentType { get; set; }
    public string? PaymentModuleCode { get; set; }
    public string? ShippingModuleCode { get; set; }
    public bool CustomerAgreed { get; set; }
    public bool ConfirmedAddress { get; set; }
    public string? Locale { get; set; }
    public AddressSnapshot? BillingAddress { get; set; }
    public AddressSnapshot? DeliveryAddress { get; set; }
    public List<OrderLine> Lines { get; } = [];
    public List<OrderTotal> Totals { get; } = [];
    public List<OrderAttribute> Attributes { get; } = [];
    public List<OrderHistory> History { get; } = [];
    public List<DownloadEntitlement> Downloads { get; } = [];
}

public sealed record AddressSnapshot(string FirstName, string LastName, string? Company, string Address,
    string City, string? State, string CountryCode, string? ZoneCode, string PostalCode, string? Telephone);
public sealed class OrderLine { public long Id { get; init; } public string Sku { get; init; } = ""; public string ProductName { get; init; } = ""; public int Quantity { get; init; } public decimal OneTimeCharge { get; init; } public bool IsDigital { get; init; } public List<LineAttribute> Attributes { get; } = []; public List<LinePrice> Prices { get; } = []; }
public sealed record LineAttribute(long OptionId, long OptionValueId, string Name, string Value, decimal Price, bool Free, decimal? Weight);
public sealed record LinePrice(string Code, string? Name, decimal Price, decimal? SpecialPrice, DateTimeOffset? SpecialStartDate, DateTimeOffset? SpecialEndDate, bool DefaultPrice);
public sealed record OrderTotal(long Id, string Code, string? Title, string? Text, decimal Value, string? Module, string? Type, string? ValueType, int SortOrder, bool IsRefund);
public sealed record OrderAttribute(string Identifier, string Value);
public sealed record OrderHistory(long Id, long OrderId, string? Status, DateTimeOffset DateAdded, string? Comments, string? ActorId, string Source, bool CustomerNotified);
public sealed record DownloadEntitlement(long Id, long OrderId, string ProductName, string FileName, int DownloadCount, int ExpiryDays, string AccessState, DateTimeOffset? ExpiresAt);
public sealed record PaymentOutcome(string TransactionId, long OrderId, string Action, string Status, decimal Amount, string Currency, string? PaymentReference, DateTimeOffset OccurredAt);
public sealed record Fulfillment(Guid Id, long OrderId, string Status, string? CarrierReference, DateTimeOffset LastUpdatedAt);

public sealed record Submission(string SubmissionId, long? CustomerId, string CustomerEmailAddress, long StoreId,
    string Currency, decimal Total, IReadOnlyList<SubmissionLine> Lines, IReadOnlyList<SubmissionTotal> Totals,
    AddressSnapshot? BillingAddress, AddressSnapshot? DeliveryAddress, string? PaymentType,
    string? PaymentModuleCode, string? ShippingModuleCode, bool CustomerAgreed, string? Locale,
    string? PaymentToken, string? ReservationId, string? Comments);
public sealed record SubmissionLine(string Sku, string ProductName, int Quantity, decimal UnitPrice,
    IReadOnlyList<SubmissionAttribute> Attributes, IReadOnlyList<SubmissionPrice> Prices, string? DigitalFileName, int DownloadExpiryDays = 31);
public sealed record SubmissionAttribute(long OptionId, long OptionValueId, string Name, string Value, decimal Price, bool Free, decimal? Weight);
public sealed record SubmissionPrice(string Code, string? Name, decimal Price, decimal? SpecialPrice, DateTimeOffset? SpecialStartDate, DateTimeOffset? SpecialEndDate, bool DefaultPrice);
public sealed record SubmissionTotal(string Code, string? Title, string? Text, decimal Value, string? Module, string? Type, string? ValueType, int SortOrder);
public sealed record InvoiceState(long OrderId, string? RequestId, string Status, string? ArtifactUrl, DateTimeOffset? GeneratedAt);
public sealed record CustomerSnapshotUpdateRequest(string EmailAddress, AddressSnapshot BillingAddress, AddressSnapshot DeliveryAddress);

public static class PrincipalExtensions
{
    public static Guid? SubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    public static long? SubjectNumber(this ClaimsPrincipal principal)
    {
        if (long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id)) return id;
        return principal.SubjectId() is { } guid ? BitConverter.ToInt64(guid.ToByteArray(), 0) & long.MaxValue : null;
    }
    public static string Kind(this ClaimsPrincipal principal) => principal.FindFirstValue("kind") ?? "";
    public static bool HasRole(this ClaimsPrincipal principal, params string[] roles) =>
        roles.Any(role => principal.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") &&
            c.Value.Equals(role, StringComparison.OrdinalIgnoreCase)));
}

public static class StatusValues
{
    private static readonly ConcurrentDictionary<object, string> Values = new();
    public static void Set(object key, string value) => Values[key] = value;
    public static string Get(object key, string fallback) => Values.TryGetValue(key, out var value) ? value : fallback;
}

public sealed class ContractStatusJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type) => type == typeof(OrderStatusDto) || type == typeof(PaymentStatusDto) || type == typeof(FulfillmentStatusDto);
    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(StatusConverter<>).MakeGenericType(type), nonPublic: true)!;
    private sealed class StatusConverter<T> : JsonConverter<T> where T : class, new()
    {
        public override T? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            var value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            var result = new T();
            StatusValues.Set(result, value ?? "Ordered");
            return result;
        }
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(StatusValues.Get(value, typeof(T) == typeof(OrderStatusDto) ? "Ordered" :
                typeof(T) == typeof(PaymentStatusDto) ? "Pending" : "NotRequested"));
    }
}

public static class DtoMapper
{
    public static OrderDto Order(Order value, bool includeChildren = true) => new()
    {
        OrderId = value.OrderId, TenantId = value.TenantId, StoreId = value.StoreId, CustomerId = value.CustomerId,
        CustomerEmailAddress = value.CustomerEmailAddress, Status = OrderStatus(value.Status),
        PaymentStatus = PaymentStatus(value.PaymentStatus), FulfillmentStatus = FulfillmentStatus(value.FulfillmentStatus),
        CurrencyCode = value.CurrencyCode, Total = value.Total, RefundedAmount = value.RefundedAmount,
        RefundableAmount = value.RefundableAmount, DatePurchased = value.DatePurchased.ToString("O"),
        OrderDateFinished = value.OrderDateFinished?.ToString("O"), PaymentType = value.PaymentType,
        PaymentModuleCode = value.PaymentModuleCode, ShippingModuleCode = value.ShippingModuleCode,
        CustomerAgreed = value.CustomerAgreed, ConfirmedAddress = value.ConfirmedAddress, Locale = value.Locale,
        BillingAddress = Address(value.BillingAddress), DeliveryAddress = Address(value.DeliveryAddress),
        Lines = includeChildren ? value.Lines.Select(Line).ToList() : [],
        Totals = includeChildren ? value.Totals.Select(Total).ToList() : [],
        Attributes = includeChildren ? value.Attributes.Select(x => new OrderAttributeDto { Identifier = x.Identifier, Value = x.Value }).ToList() : [],
        History = includeChildren ? value.History.Select(HistoryEntry).ToList() : [],
        Downloads = includeChildren ? value.Downloads.Select(Download).ToList() : []
    };
    public static AddressSnapshotDto? Address(AddressSnapshot? a) => a is null ? null : new()
    { FirstName = a.FirstName, LastName = a.LastName, Company = a.Company, Address = a.Address, City = a.City, State = a.State, CountryCode = a.CountryCode, ZoneCode = a.ZoneCode, PostalCode = a.PostalCode, Telephone = a.Telephone };
    public static OrderLineDto Line(OrderLine x) => new() { OrderProductId = x.Id, Sku = x.Sku, ProductName = x.ProductName, Quantity = x.Quantity, OneTimeCharge = x.OneTimeCharge, Attributes = x.Attributes.Select(a => new OrderLineAttributeDto { OptionId = a.OptionId, OptionValueId = a.OptionValueId, Name = a.Name, Value = a.Value, Price = a.Price, Free = a.Free, Weight = a.Weight }).ToList(), Prices = x.Prices.Select(p => new OrderLinePriceDto { Code = p.Code, Name = p.Name, Price = p.Price, SpecialPrice = p.SpecialPrice, SpecialStartDate = p.SpecialStartDate?.ToString("O"), SpecialEndDate = p.SpecialEndDate?.ToString("O"), DefaultPrice = p.DefaultPrice }).ToList() };
    public static OrderTotalDto Total(OrderTotal x) => new() { OrderTotalId = x.Id, Code = x.Code, Title = x.Title, Text = x.Text, Value = x.Value, Module = x.Module, Type = x.Type, ValueType = x.ValueType, SortOrder = x.SortOrder, IsRefund = x.IsRefund };
    public static OrderHistoryEntryDto HistoryEntry(OrderHistory x) => new() { HistoryId = x.Id, OrderId = x.OrderId, Status = x.Status is null ? null : OrderStatus(x.Status), DateAdded = x.DateAdded.ToString("O"), Comments = x.Comments, ActorId = x.ActorId, Source = x.Source, CustomerNotified = x.CustomerNotified };
    public static DownloadEntitlementDto Download(DownloadEntitlement x) => new() { DownloadId = x.Id, OrderId = x.OrderId, ProductName = x.ProductName, FileName = x.FileName, DownloadCount = x.DownloadCount, DownloadExpiryDays = x.ExpiryDays, AccessState = x.AccessState switch { "AVAILABLE" => "Available", "EXPIRED" => "Expired", "REVOKED" => "Revoked", _ => x.AccessState }, ExpiresAt = x.ExpiresAt?.ToString("O") };
    public static OrderStatusDto OrderStatus(string value) { var x = new OrderStatusDto(); StatusValues.Set(x, ToPascal(value)); return x; }
    public static PaymentStatusDto PaymentStatus(string value) { var x = new PaymentStatusDto(); StatusValues.Set(x, ToPascal(value)); return x; }
    public static FulfillmentStatusDto FulfillmentStatus(string value) { var x = new FulfillmentStatusDto(); StatusValues.Set(x, ToPascal(value)); return x; }
    private static string ToPascal(string value) => value.Length == 0 ? value : value[0] + value[1..].ToLowerInvariant();
}
