using ERP.Domain.Entities;

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
    Task<bool> ExistsAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<int?> GetNextOfferNumberForDateAsync(int offerDate, int companyId, CancellationToken cancellationToken = default);
}
