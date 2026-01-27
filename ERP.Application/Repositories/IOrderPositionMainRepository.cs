using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Interfejs repozytorium dla pozycji zamówienia z tabeli pozyjezamowienia
/// </summary>
public interface IOrderPositionMainRepository
{
    Task<OrderPositionMainDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderPositionMainDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderPositionMainDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
