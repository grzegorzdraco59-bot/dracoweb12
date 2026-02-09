using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Repozytorium odczytu pozycji zamówienia z widoku pozycjezamowienia_V.
/// </summary>
public interface IOrderPositionRepository
{
    Task<IEnumerable<OrderPositionRow>> GetByOrderIdAsync(int companyId, int orderId, CancellationToken cancellationToken = default);
}
