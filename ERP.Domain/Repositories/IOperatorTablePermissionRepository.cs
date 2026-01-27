using ERP.Domain.Entities;

namespace ERP.Domain.Repositories;

/// <summary>
/// Interfejs repozytorium dla encji OperatorTablePermission
/// </summary>
public interface IOperatorTablePermissionRepository
{
    Task<OperatorTablePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperatorTablePermission?> GetByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default);
    Task<IEnumerable<OperatorTablePermission>> GetByOperatorIdAsync(int operatorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OperatorTablePermission>> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(int operatorId, string tableName, string permissionType, CancellationToken cancellationToken = default);
    Task<int> AddOrUpdateAsync(OperatorTablePermission permission, CancellationToken cancellationToken = default);
    Task UpdateAsync(OperatorTablePermission permission, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int operatorId, string tableName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAvailableTablesAsync(CancellationToken cancellationToken = default);
}
