namespace ERP.Domain.Enums;

/// <summary>
/// Stan oferty (nagłówek aoferty). Zgodne z FAZA4_STANY_DOKUMENTOW.
/// </summary>
public enum OfferStatus
{
    Draft = 0,
    Sent,
    Accepted,
    Rejected,
    Cancelled
}

/// <summary>
/// Konwersja DB VARCHAR &lt;-&gt; OfferStatus (z walidacją; nieprawidłowa wartość → Draft).
/// </summary>
public static class OfferStatusMapping
{
    public static OfferStatus FromDb(string? value) =>
        Enum.TryParse<OfferStatus>(value, true, out var r) ? r : OfferStatus.Draft;

    public static string ToDb(OfferStatus status) => status.ToString();

    /// <summary>Dozwolone przejścia (FAZA4_STANY_DOKUMENTOW).</summary>
    public static bool IsTransitionAllowed(OfferStatus from, OfferStatus to)
    {
        return (from, to) switch
        {
            (OfferStatus.Draft, OfferStatus.Sent) => true,
            (OfferStatus.Draft, OfferStatus.Cancelled) => true,
            (OfferStatus.Sent, OfferStatus.Accepted) => true,
            (OfferStatus.Sent, OfferStatus.Rejected) => true,
            (OfferStatus.Sent, OfferStatus.Cancelled) => true,
            _ => false
        };
    }
}
