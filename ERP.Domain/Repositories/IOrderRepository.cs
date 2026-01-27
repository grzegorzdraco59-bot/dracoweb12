using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla zamówień (zamowieniahala)
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
