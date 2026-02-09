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
    /// Przelicza nagłówek oferty: SUM(netto_poz), SUM(vat_poz), SUM(brutto_poz) z ofertypozycje
    /// i zapisuje do oferty.sum_netto, oferty.sum_vat, oferty.sum_brutto. Wywołać po zmianie pozycji oraz przed generowaniem PDF.
    /// </summary>
    Task RecalcOfferTotalsAsync(int offerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Przelicza sum_netto, sum_vat, sum_brutto na podstawie pozycji (cena_netto * ilosc, stawka_vat).
    /// Oferta bez pozycji → sumy = 0. Wywołać po dodaniu/edycji/usunięciu pozycji.
    /// </summary>
    Task RecalculateOfferTotalsAsync(int offerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Odczytuje sum_brutto oferty z bazy (do odświeżenia UI bez przeładowania listy).
    /// </summary>
    Task<decimal> GetSumBruttoAsync(int offerId, CancellationToken cancellationToken = default);
}
