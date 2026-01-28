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

    public async Task<List<OperatorCompanyDto>> GetByUserIdAsync(int userId)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand(
                "SELECT id, id_operatora, id_firmy, rola FROM operatorfirma WHERE id_operatora = @UserId",
                connection);
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

    public async Task<OperatorCompanyDto?> GetByIdAsync(int id)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand(
                "SELECT id, id_operatora, id_firmy, rola FROM operatorfirma WHERE id = @Id",
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
                "INSERT INTO operatorfirma (id_operatora, id_firmy, rola) VALUES (@UserId, @CompanyId, @RoleId); SELECT LAST_INSERT_ID();",
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
                "UPDATE operatorfirma SET id_operatora = @UserId, id_firmy = @CompanyId, rola = @RoleId WHERE id = @Id",
                connection);
            command.Parameters.AddWithValue("@Id", dto.Id);
            command.Parameters.AddWithValue("@UserId", dto.UserId);
            command.Parameters.AddWithValue("@CompanyId", dto.CompanyId);
            command.Parameters.AddWithValue("@RoleId", dto.RoleId ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var connection = await _context.CreateConnectionAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        await using (connection)
        {
            var command = new MySqlCommand("DELETE FROM operatorfirma WHERE id = @Id", connection);
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

        return new OperatorCompanyDto
        {
            Id = id,
            UserId = userId,
            CompanyId = companyId,
            RoleId = roleId
        };
    }
}
