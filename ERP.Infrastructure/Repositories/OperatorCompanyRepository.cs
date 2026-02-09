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

    /// <summary>
    /// Pobiera rekordy dla firmy (wymagane) i opcjonalnie dla operatora.
    /// WHERE id_firmy = @CompanyId [AND id_operatora = @OperatorId gdy operatorId ma wartość].
    /// Bez LIMIT 1, bez operatorfirma.id = @OperatorId.
    /// </summary>
    /// <param name="companyId">ID firmy (wymagane).</param>
    /// <param name="operatorId">ID operatora (opcjonalne, 0 lub null = wszystkie).</param>
    /// <param name="includeInactive">Czy uwzględniać IsActive=0.</param>
    public async Task<List<OperatorCompanyDto>> GetByCompanyIdAsync(int companyId, int? operatorId = null, bool includeInactive = false)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var sql = "SELECT id, id_operatora, id_firmy, rola, IsActive FROM `operatorfirma` WHERE id_firmy = @CompanyId";
            if (operatorId.HasValue && operatorId.Value > 0)
                sql += " AND id_operatora = @OperatorId";
            var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CompanyId", companyId);
            if (operatorId.HasValue && operatorId.Value > 0)
                command.Parameters.AddWithValue("@OperatorId", operatorId.Value);

            var list = new List<OperatorCompanyDto>();
            await using var reader = await command.ExecuteReaderWithDiagnosticsAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapToDto(reader));
            }
            return list;
        }
    }

    /// <param name="includeInactive">Jeśli false (domyślnie), zwracane są tylko rekordy z IsActive=1.</param>
    public async Task<List<OperatorCompanyDto>> GetByUserIdAsync(int userId, bool includeInactive = false)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var sql = "SELECT id, id_operatora, id_firmy, rola, IsActive FROM `operatorfirma` WHERE id_operatora = @UserId";
            var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var list = new List<OperatorCompanyDto>();
            await using var reader = await command.ExecuteReaderWithDiagnosticsAsync();
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
            var sql = "SELECT 1 FROM `operatorfirma` WHERE id_operatora = @UserId AND id_firmy = @CompanyId";
            if (excludeId.HasValue)
                sql = "SELECT 1 FROM `operatorfirma` WHERE id_operatora = @UserId AND id_firmy = @CompanyId AND id != @ExcludeId";
            var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@CompanyId", companyId);
            if (excludeId.HasValue)
                command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);
            var result = await command.ExecuteScalarWithDiagnosticsAsync();
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
                "SELECT id, id_operatora, id_firmy, rola, IsActive FROM `operatorfirma` WHERE id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderWithDiagnosticsAsync();
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
                "INSERT INTO `operatorfirma` (id_operatora, id_firmy, rola, IsActive) VALUES (@UserId, @CompanyId, @RoleId, 1); SELECT LAST_INSERT_ID();",
                connection);
            command.Parameters.AddWithValue("@UserId", dto.UserId);
            command.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
            command.Parameters.AddWithValue("@RoleId", dto.RoleId ?? (object)DBNull.Value);

            var newId = await command.ExecuteInsertAndGetIdAsync();
            return (int)newId;
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
                "UPDATE `operatorfirma` SET id_operatora = @UserId, id_firmy = @CompanyId, rola = @RoleId, IsActive = @IsActive WHERE id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", dto.Id);
            command.Parameters.AddWithValue("@UserId", dto.UserId);
            command.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
            command.Parameters.AddWithValue("@RoleId", dto.RoleId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", dto.IsActive ? 1 : 0);

            await command.ExecuteNonQueryWithDiagnosticsAsync();
        }
    }

    public async Task DeactivateAsync(int id)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand("UPDATE `operatorfirma` SET IsActive = 0 WHERE id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryWithDiagnosticsAsync();
        }
    }

    public async Task ActivateAsync(int id)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand("UPDATE `operatorfirma` SET IsActive = 1 WHERE id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryWithDiagnosticsAsync();
        }
    }

    private static OperatorCompanyDto MapToDto(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id"));
        var userId = reader.GetInt32(reader.GetOrdinal("id_operatora"));
        var companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        var roleOrdinal = reader.GetOrdinal("rola");
        var roleId = reader.IsDBNull(roleOrdinal) ? (int?)null : reader.GetInt32(roleOrdinal);
        var isActive = SafeGetIsActive(reader);

        return new OperatorCompanyDto
        {
            Id = id,
            UserId = userId,
            CompanyId = companyId,
            RoleId = roleId,
            IsActive = isActive
        };
    }

    private static bool SafeGetIsActive(MySqlDataReader reader)
    {
        try
        {
            var ord = reader.GetOrdinal("IsActive");
            return !reader.IsDBNull(ord) && reader.GetByte(ord) != 0;
        }
        catch (ArgumentException) { return true; }
        catch (IndexOutOfRangeException) { return true; }
    }
}
