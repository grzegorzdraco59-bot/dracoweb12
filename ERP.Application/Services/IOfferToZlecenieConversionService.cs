namespace ERP.Application.Services;

/// <summary>
/// Konwersja oferty (oferty + ofertypozycje) na zlecenie produkcyjne (zlecenia + pozycjezlecenia).
/// Tworzy nagłówek zlecenia, kopiuje pozycje, ustawia oferty.do_zlecenia = 1.
/// </summary>
public interface IOfferToZlecenieConversionService
{
    /// <summary>
    /// Tworzy zlecenie z oferty: nagłówek + pozycje. Ustawia oferty.do_zlecenia = 1.
    /// </summary>
    /// <param name="offerId">ID oferty (oferty.id)</param>
    /// <param name="companyId">ID firmy</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>ID nowo utworzonego zlecenia (zlecenia.id)</returns>
    Task<int> CopyOfferToZlecenieAsync(int offerId, int companyId, CancellationToken cancellationToken = default);
}
