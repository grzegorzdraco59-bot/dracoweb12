using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Repozytorium odczytu nagłówków faktur (tabela faktury).
/// </summary>
public interface IInvoiceRepository
{
    Task<IEnumerable<InvoiceDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>Pobiera fakturę po ID (dla kopiowania).</summary>
    Task<InvoiceDto?> GetByIdAsync(long invoiceId, int companyId, CancellationToken cancellationToken = default);

    /// <summary>Dokumenty powiązane z ofertą – 1 zapytanie po nagłówki (doc_type, doc_full_no, data_faktury, sum_brutto).</summary>
    Task<IEnumerable<OfferDocumentDto>> GetDocumentsByOfferIdAsync(int offerId, int companyId, CancellationToken cancellationToken = default);

    /// <summary>Kolejny numer faktury dla (skrot_nazwa_faktury, data_faktury) w danej firmie. Skrót WYŁĄCZNIE z skrot_nazwa_faktury.</summary>
    Task<int> GetNextInvoiceNumberAsync(int companyId, string skrotNazwaFaktury, int dataFaktury, CancellationToken cancellationToken = default);

    /// <summary>Aktualizacja kontrahenta faktury (odbiorca_*).</summary>
    Task UpdateRecipientAsync(long invoiceId, int companyId, int? odbiorcaId, string? odbiorcaNazwa, string? odbiorcaMail, string? waluta, CancellationToken cancellationToken = default);
}
