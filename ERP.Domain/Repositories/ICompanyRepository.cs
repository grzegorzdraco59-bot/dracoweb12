using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla encji Company (firmy)
/// </summary>
public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Company>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Company company, CancellationToken cancellationToken = default);
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
