using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Repozytorium odczytu nagłówków faktur (tabela faktury).
/// </summary>
public interface IInvoiceRepository
{
    Task<IEnumerable<InvoiceDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Dokumenty powiązane z ofertą – 1 zapytanie po nagłówki (doc_type, doc_full_no, data_faktury, sum_brutto).</summary>
    Task<IEnumerable<OfferDocumentDto>> GetDocumentsByOfferIdAsync(int offerId, int companyId, CancellationToken cancellationToken = default);
}
