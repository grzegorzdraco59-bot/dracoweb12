using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla encji Odbiorca (Customer)
/// Repozytoria zawierają tylko operacje CRUD - logika biznesowa jest w warstwie Application
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByKontrahentIdAsync(int kontrahentId, int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByNameAsync(string name, int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, int companyId, CancellationToken cancellationToken = default);
}