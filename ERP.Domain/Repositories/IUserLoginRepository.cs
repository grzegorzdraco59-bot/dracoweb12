using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla encji UserLogin (operator_login)
/// </summary>
public interface IUserLoginRepository
{
    Task<UserLogin?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserLogin?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<UserLogin?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(UserLogin userLogin, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserLogin userLogin, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string login, CancellationToken cancellationToken = default);
}
