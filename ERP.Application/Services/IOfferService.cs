using ERP.Domain.Entities;
using ERP.Domain.Enums;

namespace ERP.Application.Services;


/// <summary>
/// Serwis ofert – operacje z walidacją statusu (FAZA4: edycja/usuwa nie tylko w Draft).
/// </summary>
public interface IOfferService
{
    Task<Offer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Offer>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int?> GetNextOfferNumberForDateAsync(int offerDate, int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Offer offer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Offer offer, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task SetStatusAsync(int offerId, int companyId, OfferStatus newStatus, CancellationToken cancellationToken = default);

    Task<IEnumerable<OfferPosition>> GetPositionsByOfferIdAsync(int offerId, CancellationToken cancellationToken = default);
    Task<OfferPosition?> GetPositionByIdAsync(int positionId, CancellationToken cancellationToken = default);
    Task<int> AddPositionAsync(OfferPosition position, CancellationToken cancellationToken = default);
    Task UpdatePositionAsync(OfferPosition position, CancellationToken cancellationToken = default);
    Task DeletePositionAsync(int positionId, CancellationToken cancellationToken = default);
}
