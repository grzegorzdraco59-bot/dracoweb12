using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium UserCompany (operatorfirma) używająca MySqlConnector
/// </summary>
public class UserCompanyRepository : IUserCompanyRepository
{
    private readonly DatabaseContext _context;

    public UserCompanyRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserCompany?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, id_firmy, rola FROM operatorfirma WHERE id = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToUserCompany(reader);
        }

        return null;
    }

    public async Task<UserCompany?> GetByUserAndCompanyAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, id_firmy, rola FROM operatorfirma " +
            "WHERE id_operatora = @UserId AND id_firmy = @CompanyId LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToUserCompany(reader);
        }

        return null;
    }

    public async Task<IEnumerable<UserCompany>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userCompanies = new List<UserCompany>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, id_firmy, rola FROM operatorfirma " +
            "WHERE id_operatora = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            userCompanies.Add(MapToUserCompany(reader));
        }

        return userCompanies;
    }

    public async Task<IEnumerable<UserCompany>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var userCompanies = new List<UserCompany>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, id_firmy, rola FROM operatorfirma " +
            "WHERE id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            userCompanies.Add(MapToUserCompany(reader));
        }

        return userCompanies;
    }

    public async Task<int> AddAsync(UserCompany userCompany, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO operatorfirma (id_operatora, id_firmy, rola) " +
            "VALUES (@UserId, @CompanyId, @RoleId); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@UserId", userCompany.UserId);
        command.Parameters.AddWithValue("@CompanyId", userCompany.CompanyId);
        command.Parameters.AddWithValue("@RoleId", userCompany.RoleId ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(UserCompany userCompany, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE operatorfirma SET id_operatora = @UserId, id_firmy = @CompanyId, rola = @RoleId " +
            "WHERE id = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", userCompany.Id);
        command.Parameters.AddWithValue("@UserId", userCompany.UserId);
        command.Parameters.AddWithValue("@CompanyId", userCompany.CompanyId);
        command.Parameters.AddWithValue("@RoleId", userCompany.RoleId ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int userId, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM operatorfirma WHERE id_operatora = @UserId AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM operatorfirma WHERE id = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static UserCompany MapToUserCompany(MySqlDataReader reader)
    {
        var idOrdinal = reader.GetOrdinal("id");
        var userIdOrdinal = reader.GetOrdinal("id_operatora");
        var companyIdOrdinal = reader.GetOrdinal("id_firmy");
        var roleIdOrdinal = reader.GetOrdinal("rola");

        if (reader.IsDBNull(idOrdinal))
            throw new InvalidOperationException("Pole 'id' nie może być NULL w tabeli operatorfirma.");
        if (reader.IsDBNull(userIdOrdinal))
            throw new InvalidOperationException("Pole 'id_operatora' nie może być NULL w tabeli operatorfirma.");
        if (reader.IsDBNull(companyIdOrdinal))
            throw new InvalidOperationException("Pole 'id_firmy' nie może być NULL w tabeli operatorfirma.");

        var id = reader.GetInt32(idOrdinal);
        var userId = reader.GetInt32(userIdOrdinal);
        var companyId = reader.GetInt32(companyIdOrdinal);
        var roleId = reader.IsDBNull(roleIdOrdinal) ? (int?)null : reader.GetInt32(roleIdOrdinal);

        var userCompany = new UserCompany(userId, companyId, roleId);
        
        // Ustawiamy Id używając refleksji
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        idProperty.SetValue(userCompany, id);

        return userCompany;
    }
}
