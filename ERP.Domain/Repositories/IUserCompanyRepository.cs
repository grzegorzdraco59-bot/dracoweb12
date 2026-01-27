using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla encji UserCompany (operatorfirma)
/// </summary>
public interface IUserCompanyRepository
{
    Task<UserCompany?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserCompany?> GetByUserAndCompanyAsync(int userId, int companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserCompany>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserCompany>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(UserCompany userCompany, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserCompany userCompany, CancellationToken cancellationToken = default);
    Task DeleteAsync(int userId, int companyId, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(int id, CancellationToken cancellationToken = default);
}
