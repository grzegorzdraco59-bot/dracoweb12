using ERP.Application.DTOs;
using ERP.Domain.Enums;

namespace ERP.Application.Services;

/// <summary>
/// Serwis zamówień głównych (nagłówek zamowienia + pozycje) – walidacja statusu (FAZA4: tylko Draft).
/// </summary>
public interface IOrderMainService
{
    Task<OrderMainDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderMainDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderMainDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> AddAsync(OrderMainDto order, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderMainDto order, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task SetStatusAsync(int orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tworzy zamówienie z oferty (Accepted) atomowo – nagłówek + pozycje. Nowe zamówienie ma status Draft.
    /// </summary>
    /// <returns>Id utworzonego zamówienia.</returns>
    Task<int> CreateFromOfferAsync(int offerId, CancellationToken cancellationToken = default);

    Task<IEnumerable<OrderPositionMainDto>> GetPositionsByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderPositionMainDto?> GetPositionByIdAsync(int positionId, CancellationToken cancellationToken = default);
    Task<int> AddPositionAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default);
    Task UpdatePositionAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default);
    Task DeletePositionAsync(int positionId, CancellationToken cancellationToken = default);
}
