namespace ERP.Domain.Enums;

/// <summary>
/// Stan zamówienia (nagłówek zamowienia). Zgodne z FAZA4_STANY_DOKUMENTOW.
/// </summary>
public enum OrderStatus
{
    Draft = 0,
    Confirmed,
    InProgress,
    Shipped,
    Completed,
    Cancelled
}

/// <summary>
/// Konwersja DB VARCHAR &lt;-&gt; OrderStatus (z walidacją; nieprawidłowa wartość → Draft).
/// </summary>
public static class OrderStatusMapping
{
    public static OrderStatus FromDb(string? value) =>
        Enum.TryParse<OrderStatus>(value, true, out var r) ? r : OrderStatus.Draft;

    public static string ToDb(OrderStatus status) => status.ToString();

    /// <summary>Dozwolone przejścia (FAZA4_STANY_DOKUMENTOW).</summary>
    public static bool IsTransitionAllowed(OrderStatus from, OrderStatus to)
    {
        return (from, to) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.InProgress) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.InProgress, OrderStatus.Shipped) => true,
            (OrderStatus.InProgress, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Completed) => true,
            _ => false
        };
    }
}
