using ERP.Application.DTOs;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;

namespace ERP.Application.Services;

/// <summary>
/// Implementacja serwisu do zarządzania uprawnieniami operatorów do tabel
/// </summary>
public class OperatorPermissionService : IOperatorPermissionService
{
    private readonly IOperatorTablePermissionRepository _permissionRepository;
    private readonly IUserRepository _userRepository;

    public OperatorPermissionService(
        IOperatorTablePermissionRepository permissionRepository,
        IUserRepository userRepository)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<OperatorTablePermissionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken);
        return permission != null ? await MapToDtoAsync(permission, cancellationToken) : null;
    }

    public async Task<OperatorTablePermissionDto?> GetByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByOperatorAndTableAsync(operatorId, tableName, cancellationToken);
        return permission != null ? await MapToDtoAsync(permission, cancellationToken) : null;
    }

    public async Task<IEnumerable<OperatorTablePermissionDto>> GetByOperatorIdAsync(int operatorId, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetByOperatorIdAsync(operatorId, cancellationToken);
        var dtos = new List<OperatorTablePermissionDto>();
        
        foreach (var permission in permissions)
        {
            dtos.Add(await MapToDtoAsync(permission, cancellationToken));
        }
        
        return dtos;
    }

    public async Task<IEnumerable<OperatorTablePermissionDto>> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetByTableNameAsync(tableName, cancellationToken);
        var dtos = new List<OperatorTablePermissionDto>();
        
        foreach (var permission in permissions)
        {
            dtos.Add(await MapToDtoAsync(permission, cancellationToken));
        }
        
        return dtos;
    }

    public async Task<bool> HasPermissionAsync(int operatorId, string tableName, string permissionType, CancellationToken cancellationToken = default)
    {
        return await _permissionRepository.HasPermissionAsync(operatorId, tableName, permissionType, cancellationToken);
    }

    public async Task<OperatorTablePermissionDto> SetPermissionAsync(
        int operatorId, 
        string tableName, 
        bool canSelect, 
        bool canInsert, 
        bool canUpdate, 
        bool canDelete, 
        CancellationToken cancellationToken = default)
    {
        var permission = new OperatorTablePermission(operatorId, tableName, canSelect, canInsert, canUpdate, canDelete);
        var id = await _permissionRepository.AddOrUpdateAsync(permission, cancellationToken);
        
        var createdPermission = await _permissionRepository.GetByIdAsync(id, cancellationToken);
        if (createdPermission == null)
            throw new InvalidOperationException("Nie udało się utworzyć lub zaktualizować uprawnienia.");

        return await MapToDtoAsync(createdPermission, cancellationToken);
    }

    public async Task UpdatePermissionAsync(int id, bool canSelect, bool canInsert, bool canUpdate, bool canDelete, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken);
        if (permission == null)
            throw new ArgumentException($"Uprawnienie o ID {id} nie zostało znalezione.", nameof(id));

        permission.UpdatePermissions(canSelect, canInsert, canUpdate, canDelete);
        await _permissionRepository.UpdateAsync(permission, cancellationToken);
    }

    public async Task DeletePermissionAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _permissionRepository.GetByIdAsync(id, cancellationToken) == null)
            throw new ArgumentException($"Uprawnienie o ID {id} nie zostało znalezione.", nameof(id));

        await _permissionRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task DeletePermissionByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default)
    {
        await _permissionRepository.DeleteByOperatorAndTableAsync(operatorId, tableName, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAvailableTablesAsync(CancellationToken cancellationToken = default)
    {
        // Pobierz listę tabel z bazy danych używając information_schema
        // Użyjemy DatabaseContext przez repozytorium
        return await _permissionRepository.GetAvailableTablesAsync(cancellationToken);
    }

    private async Task<OperatorTablePermissionDto> MapToDtoAsync(OperatorTablePermission permission, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(permission.OperatorId, cancellationToken);
        
        return new OperatorTablePermissionDto
        {
            Id = permission.Id,
            OperatorId = permission.OperatorId,
            OperatorName = user?.FullName ?? string.Empty,
            TableName = permission.TableName,
            CanSelect = permission.CanSelect,
            CanInsert = permission.CanInsert,
            CanUpdate = permission.CanUpdate,
            CanDelete = permission.CanDelete,
            CreatedAt = permission.CreatedAt,
            UpdatedAt = permission.UpdatedAt
        };
    }
}
