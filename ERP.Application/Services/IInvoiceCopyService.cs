namespace ERP.Application.Services;

/// <summary>
/// Serwis kopiowania faktury do nowego dokumentu (FVZ, FV).
/// Kopiuje nagłówek i pozycje do nowej faktury z doc_type FVZ lub FV.
/// </summary>
public interface IInvoiceCopyService
{
    /// <summary>
    /// Kopiuje fakturę do nowego dokumentu FVZ (faktura zaliczkowa).
    /// </summary>
    /// <param name="sourceInvoiceId">ID źródłowej faktury</param>
    /// <param name="companyId">ID firmy</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>ID nowej faktury</returns>
    Task<int> CopyInvoiceToFvzAsync(long sourceInvoiceId, int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kopiuje fakturę do nowego dokumentu FV (faktura VAT).
    /// </summary>
    /// <param name="sourceInvoiceId">ID źródłowej faktury</param>
    /// <param name="companyId">ID firmy</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>ID nowej faktury</returns>
    Task<int> CopyInvoiceToFvAsync(long sourceInvoiceId, int companyId, CancellationToken cancellationToken = default);
}
