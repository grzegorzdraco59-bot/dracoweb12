using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium OperatorTablePermission używająca MySqlConnector
/// </summary>
public class OperatorTablePermissionRepository : IOperatorTablePermissionRepository
{
    private readonly DatabaseContext _context;

    public OperatorTablePermissionRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<OperatorTablePermission?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, table_name, can_select, can_insert, can_update, can_delete, CreatedAt, UpdatedAt " +
            "FROM operator_table_permissions WHERE id = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToPermission(reader);
        }

        return null;
    }

    public async Task<OperatorTablePermission?> GetByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, table_name, can_select, can_insert, can_update, can_delete, CreatedAt, UpdatedAt " +
            "FROM operator_table_permissions WHERE id_operatora = @OperatorId AND table_name = @TableName",
            connection);
        command.Parameters.AddWithValue("@OperatorId", operatorId);
        command.Parameters.AddWithValue("@TableName", tableName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToPermission(reader);
        }

        return null;
    }

    public async Task<IEnumerable<OperatorTablePermission>> GetByOperatorIdAsync(int operatorId, CancellationToken cancellationToken = default)
    {
        var permissions = new List<OperatorTablePermission>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, table_name, can_select, can_insert, can_update, can_delete, CreatedAt, UpdatedAt " +
            "FROM operator_table_permissions WHERE id_operatora = @OperatorId ORDER BY table_name",
            connection);
        command.Parameters.AddWithValue("@OperatorId", operatorId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            permissions.Add(MapToPermission(reader));
        }

        return permissions;
    }

    public async Task<IEnumerable<OperatorTablePermission>> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var permissions = new List<OperatorTablePermission>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, table_name, can_select, can_insert, can_update, can_delete, CreatedAt, UpdatedAt " +
            "FROM operator_table_permissions WHERE table_name = @TableName ORDER BY id_operatora",
            connection);
        command.Parameters.AddWithValue("@TableName", tableName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            permissions.Add(MapToPermission(reader));
        }

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(int operatorId, string tableName, string permissionType, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        
        // Używamy bezpośredniego zapytania zamiast procedury składowanej, aby uniknąć problemów z parametrami wyjściowymi
        var permission = await GetByOperatorAndTableAsync(operatorId, tableName, cancellationToken);
        
        if (permission == null)
        {
            // Jeśli nie ma rekordu uprawnień, domyślnie zwracamy false
            return false;
        }

        return permissionType.ToUpper() switch
        {
            "SELECT" => permission.CanSelect,
            "INSERT" => permission.CanInsert,
            "UPDATE" => permission.CanUpdate,
            "DELETE" => permission.CanDelete,
            _ => false
        };
    }

    public async Task<int> AddOrUpdateAsync(OperatorTablePermission permission, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "CALL sp_SetOperatorTablePermission(@OperatorId, @TableName, @CanSelect, @CanInsert, @CanUpdate, @CanDelete)",
            connection);
        
        command.Parameters.AddWithValue("@OperatorId", permission.OperatorId);
        command.Parameters.AddWithValue("@TableName", permission.TableName);
        command.Parameters.AddWithValue("@CanSelect", permission.CanSelect ? 1 : 0);
        command.Parameters.AddWithValue("@CanInsert", permission.CanInsert ? 1 : 0);
        command.Parameters.AddWithValue("@CanUpdate", permission.CanUpdate ? 1 : 0);
        command.Parameters.AddWithValue("@CanDelete", permission.CanDelete ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken);

        // Pobierz ID zaktualizowanego lub nowego rekordu
        var existing = await GetByOperatorAndTableAsync(permission.OperatorId, permission.TableName, cancellationToken);
        return existing?.Id ?? 0;
    }

    public async Task UpdateAsync(OperatorTablePermission permission, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE operator_table_permissions SET " +
            "can_select = @CanSelect, can_insert = @CanInsert, can_update = @CanUpdate, can_delete = @CanDelete " +
            "WHERE id = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", permission.Id);
        command.Parameters.AddWithValue("@CanSelect", permission.CanSelect ? 1 : 0);
        command.Parameters.AddWithValue("@CanInsert", permission.CanInsert ? 1 : 0);
        command.Parameters.AddWithValue("@CanUpdate", permission.CanUpdate ? 1 : 0);
        command.Parameters.AddWithValue("@CanDelete", permission.CanDelete ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM operator_table_permissions WHERE id = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByOperatorAndTableAsync(int operatorId, string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "CALL sp_RemoveOperatorTablePermission(@OperatorId, @TableName)",
            connection);
        command.Parameters.AddWithValue("@OperatorId", operatorId);
        command.Parameters.AddWithValue("@TableName", tableName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int operatorId, string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM operator_table_permissions WHERE id_operatora = @OperatorId AND table_name = @TableName",
            connection);
        command.Parameters.AddWithValue("@OperatorId", operatorId);
        command.Parameters.AddWithValue("@TableName", tableName);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task<IEnumerable<string>> GetAvailableTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = new List<string>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT TABLE_NAME FROM information_schema.TABLES " +
            "WHERE TABLE_SCHEMA = DATABASE() " +
            "AND TABLE_TYPE = 'BASE TABLE' " +
            "ORDER BY TABLE_NAME",
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static OperatorTablePermission MapToPermission(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id"));
        var operatorId = reader.GetInt32(reader.GetOrdinal("id_operatora"));
        var tableName = reader.GetString(reader.GetOrdinal("table_name"));
        var canSelect = reader.GetInt32(reader.GetOrdinal("can_select")) == 1;
        var canInsert = reader.GetInt32(reader.GetOrdinal("can_insert")) == 1;
        var canUpdate = reader.GetInt32(reader.GetOrdinal("can_update")) == 1;
        var canDelete = reader.GetInt32(reader.GetOrdinal("can_delete")) == 1;

        var permission = new OperatorTablePermission(operatorId, tableName, canSelect, canInsert, canUpdate, canDelete);
        
        // Ustawiamy Id używając refleksji
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        idProperty.SetValue(permission, id);

        // Ustawiamy CreatedAt i UpdatedAt
        var createdAtProperty = typeof(BaseEntity).GetProperty("CreatedAt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var updatedAtProperty = typeof(BaseEntity).GetProperty("UpdatedAt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        
        createdAtProperty.SetValue(permission, reader.GetDateTime(reader.GetOrdinal("CreatedAt")));
        
        if (!reader.IsDBNull(reader.GetOrdinal("UpdatedAt")))
        {
            updatedAtProperty.SetValue(permission, reader.GetDateTime(reader.GetOrdinal("UpdatedAt")));
        }

        return permission;
    }
}
