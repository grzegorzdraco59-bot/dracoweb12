using ERP.Application.DTOs;

namespace ERP.Application.Services;

/// <summary>
/// Interfejs serwisu aplikacyjnego dla zamówień
/// </summary>
public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> AddAsync(OrderDto orderDto, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderDto orderDto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
