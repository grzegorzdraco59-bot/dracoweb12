using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla encji User (operator)
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
