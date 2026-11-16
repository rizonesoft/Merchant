using System.Text.Json;

namespace Merchant.Core.Domain;

public static class EventTypes
{
    public const string ProductDefined = "product.defined";
    public const string SaleCompleted = "sale.completed";
}

public static class Tender
{
    public const string Cash = "cash";

    /// <summary>Card taken on a standalone reader and recorded by hand.</summary>
    public const string Card = "card";
}

/// <summary>Creates a product, or replaces its name and price when the id already exists.</summary>
public sealed record ProductDefined(Guid ProductId, string Name, long PriceCents);

public sealed record SaleLine(Guid ProductId, string Name, int Quantity, long UnitPriceCents)
{
    public long TotalCents => Quantity * UnitPriceCents;
}

public sealed record SaleCompleted(Guid SaleId, IReadOnlyList<SaleLine> Lines, long TotalCents, string Tender);

public static class ShopJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Write<T>(T payload) => JsonSerializer.Serialize(payload, Options);

    public static T Read<T>(string payload) =>
        JsonSerializer.Deserialize<T>(payload, Options)
        ?? throw new JsonException($"Empty {typeof(T).Name} payload.");
}
