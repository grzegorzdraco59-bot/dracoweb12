namespace ERP.Application.Services;

/// <summary>
/// Konwersja oferty (Accepted) na zamówienie – atomowo (nagłówek + pozycje).
/// Zgodne z docs/FAZA4_STANY_DOKUMENTOW.md (tylko oferta w stanie Accepted).
/// </summary>
public interface IOrderFromOfferConversionService
{
    /// <summary>
    /// Tworzy zamówienie z oferty (nagłówek + kopiowanie pozycji) w jednej transakcji.
    /// Oferta musi być w statusie Accepted. Nowe zamówienie ma Status = Draft.
    /// </summary>
    /// <returns>Id utworzonego zamówienia.</returns>
    Task<int> CreateFromOfferAsync(int offerId, int companyId, CancellationToken cancellationToken = default);
}
