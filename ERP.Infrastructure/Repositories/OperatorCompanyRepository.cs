using ERP.Application.DTOs;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Repozytorium CRUD dla powiązań operator–firma (tabela operatorfirma).
/// Używa tylko DatabaseContext.CreateConnectionAsync, bez UnitOfWork.
/// </summary>
public class OperatorCompanyRepository
{
    private readonly DatabaseContext _context;

    public OperatorCompanyRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <param name="includeInactive">Jeśli false (domyślnie), zwracane są tylko rekordy z IsActive=1.</param>
    public async Task<List<OperatorCompanyDto>> GetByUserIdAsync(int userId, bool includeInactive = false)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var sql = "SELECT id, id_operatora, id_firmy, rola, IsActive FROM operatorfirma WHERE id_operatora = @UserId";
            if (!includeInactive)
                sql += " AND (IsActive = 1 OR IsActive IS NULL)";
            var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var list = new List<OperatorCompanyDto>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapToDto(reader));
            }
            return list;
        }
    }

    /// <summary>
    /// Czy istnieje aktywny rekord (UserId, CompanyId). Dla EDIT podaj excludeId = bieżące Id, żeby nie liczyć samego siebie.
    /// </summary>
    public async Task<bool> ExistsActiveByUserAndCompanyAsync(int userId, int companyId, int? excludeId = null)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var sql = "SELECT 1 FROM operatorfirma WHERE id_operatora = @UserId AND id_firmy = @CompanyId AND (IsActive = 1 OR IsActive IS NULL) LIMIT 1";
            if (excludeId.HasValue)
                sql = "SELECT 1 FROM operatorfirma WHERE id_operatora = @UserId AND id_firmy = @CompanyId AND (IsActive = 1 OR IsActive IS NULL) AND id != @ExcludeId LIMIT 1";
            var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@CompanyId", companyId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
    }

    public async Task<OperatorCompanyDto?> GetByIdAsync(int id)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand(
                "SELECT id, id_operatora, id_firmy, rola, IsActive FROM operatorfirma WHERE id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToDto(reader);
            }
            return null;
        }
    }

    public async Task<int> AddAsync(OperatorCompanyDto dto)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand(
                "INSERT INTO operatorfirma (id_operatora, id_firmy, rola, IsActive) VALUES (@UserId, @CompanyId, @RoleId, 1); SELECT LAST_INSERT_ID();",
                connection);
            command.Parameters.AddWithValue("@UserId", dto.UserId);
            command.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
            command.Parameters.AddWithValue("@RoleId", dto.RoleId ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }

    public async Task UpdateAsync(OperatorCompanyDto dto)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand(
                "UPDATE operatorfirma SET id_operatora = @UserId, id_firmy = @CompanyId, rola = @RoleId, IsActive = @IsActive WHERE id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", dto.Id);
            command.Parameters.AddWithValue("@UserId", dto.UserId);
            command.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
            command.Parameters.AddWithValue("@RoleId", dto.RoleId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", dto.IsActive ? 1 : 0);

            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DeactivateAsync(int id)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand("UPDATE operatorfirma SET IsActive = 0 WHERE id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task ActivateAsync(int id)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand("UPDATE operatorfirma SET IsActive = 1 WHERE id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static OperatorCompanyDto MapToDto(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id"));
        var userId = reader.GetInt32(reader.GetOrdinal("id_operatora"));
        var companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        var roleOrdinal = reader.GetOrdinal("rola");
        var roleId = reader.IsDBNull(roleOrdinal) ? (int?)null : reader.GetInt32(roleOrdinal);
        var isActiveOrdinal = reader.GetOrdinal("IsActive");
        var isActive = !reader.IsDBNull(isActiveOrdinal) && reader.GetByte(isActiveOrdinal) != 0;

        return new OperatorCompanyDto
        {
            Id = id,
            UserId = userId,
            CompanyId = companyId,
            RoleId = roleId,
            IsActive = isActive
        };
    }
}
