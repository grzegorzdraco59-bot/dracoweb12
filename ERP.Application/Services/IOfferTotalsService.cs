namespace ERP.Application.Services;

/// <summary>
/// Przeliczanie kwot pozycji oferty i sumy brutto nagłówka.
/// Wywołać po: dodaniu/edycji/usunięciu pozycji oferty (najpierw RecalculateOfferLinesAsync, potem RecalculateSumBruttoAsync).
/// </summary>
public interface IOfferTotalsService
{
    /// <summary>
    /// Przelicza netto_poz, vat_poz, brutto_poz dla wszystkich pozycji danej oferty (wzór: ilosc * cena_netto * (1 - rabat/100), VAT, brutto).
    /// Kolumny: ilosc, Cena, Rabat, stawka_vat (VARCHAR). Nie używa id pozycji.
    /// </summary>
    Task RecalculateOfferLinesAsync(int offerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera SUM(brutto_poz) z ofertypozycje, zapisuje do oferty.sum_brutto i zwraca nową sumę.
    /// </summary>
    Task<decimal> RecalculateSumBruttoAsync(int offerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Odczytuje sum_brutto oferty z bazy (do odświeżenia UI bez przeładowania listy).
    /// </summary>
    Task<decimal> GetSumBruttoAsync(int offerId, CancellationToken cancellationToken = default);
}
