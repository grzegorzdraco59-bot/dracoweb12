namespace ERP.Application.Services;

/// <summary>
/// Przeliczanie sum_brutto oferty z pozycji (SUM(ofertypozycje.brutto_poz)).
/// Wywołać po: dodaniu/edycji/usunięciu pozycji oferty.
/// </summary>
public interface IOfferTotalsService
{
    /// <summary>
    /// Pobiera SUM(brutto_poz) z ofertypozycje dla danej oferty
    /// i zapisuje do oferty.sum_brutto. Aktualizuje tylko gdy oferta istnieje.
    /// </summary>
    Task RecalculateSumBruttoAsync(int offerId, CancellationToken cancellationToken = default);
}
