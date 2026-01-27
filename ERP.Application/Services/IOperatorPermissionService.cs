using ERP.Application.DTOs;

namespace ERP.Application.Services;

/// <summary>
/// Interfejs serwisu do zarządzania uprawnieniami operatorów do tabel
/// </summary>
public interface IOperatorPermissionService
{
    Task<OperatorTablePermissionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperatorTablePermissionDto?> GetByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default);
    Task<IEnumerable<OperatorTablePermissionDto>> GetByOperatorIdAsync(int operatorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OperatorTablePermissionDto>> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(int operatorId, string tableName, string permissionType, CancellationToken cancellationToken = default);
    Task<OperatorTablePermissionDto> SetPermissionAsync(int operatorId, string tableName, bool canSelect, bool canInsert, bool canUpdate, bool canDelete, CancellationToken cancellationToken = default);
    Task UpdatePermissionAsync(int id, bool canSelect, bool canInsert, bool canUpdate, bool canDelete, CancellationToken cancellationToken = default);
    Task DeletePermissionAsync(int id, CancellationToken cancellationToken = default);
    Task DeletePermissionByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAvailableTablesAsync(CancellationToken cancellationToken = default);
}
