using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

public interface IOfferPositionRepository
{
    Task<OfferPosition?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OfferPosition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OfferPosition>> GetByOfferIdAsync(int offerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OfferPosition>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(OfferPosition offerPosition, CancellationToken cancellationToken = default);
    Task UpdateAsync(OfferPosition offerPosition, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteByOfferIdAsync(int offerId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
