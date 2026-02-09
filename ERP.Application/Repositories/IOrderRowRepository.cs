using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Repozytorium odczytu wierszy zamówień z widoku zamowienia_V.
/// </summary>
public interface IOrderRowRepository
{
    Task<IEnumerable<OrderRow>> GetByCompanyIdAsync(int companyId, string? searchText = null, CancellationToken cancellationToken = default);
}
