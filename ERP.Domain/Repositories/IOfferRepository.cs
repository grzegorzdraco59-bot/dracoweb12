using ERP.Domain.Entities;
using ERP.Domain.Enums;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla encji Oferta (Offer)
/// Repozytoria zawierają tylko operacje CRUD - logika biznesowa jest w warstwie Application
/// </summary>
public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Offer>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Offer offer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Offer offer, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task SetStatusAsync(int id, int companyId, OfferStatus status, CancellationToken cancellationToken = default);
    Task SetFlagsAsync(int offerId, int companyId, bool? forProforma, bool? forOrder, bool forInvoice, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<int?> GetNextOfferNumberForDateAsync(int offerDate, int companyId, CancellationToken cancellationToken = default);

    /// <summary>Wyszukiwanie LIKE po kolumnach odbiorcy (odbiorca_nazwa, odbiorca_ulica, odbiorca_panstwo, odbiorca_miasto). Limit 200.</summary>
    Task<IEnumerable<Offer>> SearchByCompanyIdAsync(int companyId, string? searchText, int limit = 200, CancellationToken cancellationToken = default);
}
