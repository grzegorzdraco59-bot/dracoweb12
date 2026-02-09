namespace ERP.Application.Services;

/// <summary>
/// Konwersja oferty (oferty + ofertypozycje) na proformę FPF (faktury + pozycjefaktury).
/// Idempotencja po faktury.id_oferty + faktury.doc_type='FPF'.
/// Logika w Application – do użycia z WPF i (w przyszłości) Web bez duplikacji.
/// </summary>
public interface IOfferToFpfConversionService
{
    /// <summary>
    /// Zwraca ID proformy FPF dla oferty: istniejącej (jeśli już skonwertowano) lub nowo utworzonej.
    /// Sprawdza: SELECT Id_faktury FROM faktury WHERE id_oferty=@offerId AND doc_type='FPF' LIMIT 1.
    /// </summary>
    /// <param name="offerId">ID oferty (oferty.id)</param>
    /// <param name="companyId">ID firmy</param>
    /// <param name="userId">ID użytkownika (np. do operatora)</param>
    /// <returns>Id_faktury (istniejącej lub nowo utworzonej)</returns>
    Task<int> CopyOfferToProformaAsync(int offerId, int companyId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca ID proformy FPF dla oferty: istniejącej lub nowo utworzonej. Wersja bez userId (dla kompatybilności).
    /// </summary>
    Task<(int InvoiceId, bool CreatedNew)> CopyOfferToFpfAsync(int offerId, int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca ID faktury zaliczkowej (FVZ) dla oferty: istniejącej lub nowo utworzonej. Idempotencja po id_oferty + doc_type='FVZ'.
    /// </summary>
    Task<(int InvoiceId, bool CreatedNew)> CopyOfferToFvzAsync(int offerId, int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca ID faktury VAT (FV) dla oferty: istniejącej lub nowo utworzonej. Idempotencja po id_oferty + doc_type='FV'.
    /// </summary>
    Task<(int InvoiceId, bool CreatedNew)> CopyOfferToFvAsync(int offerId, int companyId, CancellationToken cancellationToken = default);
}
