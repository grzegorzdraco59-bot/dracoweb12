using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Role (rola) używająca MySqlConnector
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly DatabaseContext _context;

    public RoleRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_roli, nazwa FROM rola WHERE id_roli = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToRole(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = new List<Role>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_roli, nazwa FROM rola ORDER BY nazwa",
            connection);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(MapToRole(reader));
        }

        return roles;
    }

    public async Task<int> AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO rola (nazwa) VALUES (@Name); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@Name", role.Name);
        var newId = await command.ExecuteInsertAndGetIdAsync(cancellationToken);
        return (int)newId;
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE rola SET nazwa = @Name WHERE id_roli = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", role.Id);
        command.Parameters.AddWithValue("@Name", role.Name);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("SELECT COUNT(1) FROM rola WHERE id_roli = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static Role MapToRole(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id_roli"));
        var name = reader.IsDBNull(reader.GetOrdinal("nazwa")) ? string.Empty : reader.GetString(reader.GetOrdinal("nazwa"));

        var role = new Role(name);
        
        // Ustawiamy Id używając refleksji
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        idProperty.SetValue(role, id);

        return role;
    }
}
