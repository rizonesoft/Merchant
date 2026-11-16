using Merchant.Core.Events;

namespace Merchant.Core.Domain;

public sealed record Product(Guid Id, string Name, long PriceCents);

/// <summary>
/// What the shop looks like right now, derived purely from the event log. Any two stores holding
/// the same events derive the same state, whatever order the events arrived in.
/// </summary>
public sealed class ShopState
{
    private readonly Dictionary<Guid, Product> _products = [];
    private readonly List<(DateOnly Day, long TotalCents)> _sales = [];

    public IReadOnlyList<Product> Products =>
        [.. _products.Values.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.Id)];

    public long TotalCentsOn(DateOnly day) => _sales.Where(s => s.Day == day).Sum(s => s.TotalCents);

    public int SaleCountOn(DateOnly day) => _sales.Count(s => s.Day == day);

    /// <param name="events">Events in time order, as <c>SqliteEventStore.ReadAll</c> returns them.</param>
    public static ShopState From(IEnumerable<EventRecord> events)
    {
        var state = new ShopState();
        foreach (var e in events)
        {
            switch (e.Type)
            {
                case EventTypes.ProductDefined:
                    var product = ShopJson.Read<ProductDefined>(e.Payload);
                    state._products[product.ProductId] = new Product(product.ProductId, product.Name, product.PriceCents);
                    break;

                case EventTypes.SaleCompleted:
                    var sale = ShopJson.Read<SaleCompleted>(e.Payload);
                    state._sales.Add((DateOnly.FromDateTime(e.OccurredAt.LocalDateTime), sale.TotalCents));
                    break;

                // Types this build does not know come from a newer till: they stay in the log
                // and sync onward, and simply do not show here.
            }
        }
        return state;
    }
}
