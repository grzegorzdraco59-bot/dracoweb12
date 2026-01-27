using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Interfejs repozytorium dla zamówień z tabeli zamowienia
/// </summary>
public interface IOrderMainRepository
{
    Task<OrderMainDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderMainDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderMainDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(OrderMainDto order, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderMainDto order, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
