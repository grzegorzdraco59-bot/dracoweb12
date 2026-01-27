using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium UserLogin (operator_login) używająca MySqlConnector
/// </summary>
public class UserLoginRepository : IUserLoginRepository
{
    private readonly DatabaseContext _context;

    public UserLoginRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserLogin?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, login, haslohash " +
            "FROM operator_login WHERE id = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToUserLogin(reader);
        }

        return null;
    }

    public async Task<UserLogin?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, login, haslohash " +
            "FROM operator_login WHERE login = @Login LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@Login", login);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToUserLogin(reader);
        }

        return null;
    }

    public async Task<UserLogin?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, id_operatora, login, haslohash " +
            "FROM operator_login WHERE id_operatora = @UserId LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToUserLogin(reader);
        }

        return null;
    }

    public async Task<int> AddAsync(UserLogin userLogin, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO operator_login (id_operatora, login, haslohash) " +
            "VALUES (@UserId, @Login, @PasswordHash); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@UserId", userLogin.UserId);
        command.Parameters.AddWithValue("@Login", userLogin.Login);
        command.Parameters.AddWithValue("@PasswordHash", userLogin.PasswordHash);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException("Nie udało się pobrać ID nowo utworzonego rekordu operator_login.");
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(UserLogin userLogin, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE operator_login SET id_operatora = @UserId, login = @Login, haslohash = @PasswordHash " +
            "WHERE id = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", userLogin.Id);
        command.Parameters.AddWithValue("@UserId", userLogin.UserId);
        command.Parameters.AddWithValue("@Login", userLogin.Login);
        command.Parameters.AddWithValue("@PasswordHash", userLogin.PasswordHash);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("DELETE FROM operator_login WHERE id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string login, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("SELECT COUNT(1) FROM operator_login WHERE login = @Login", connection);
        command.Parameters.AddWithValue("@Login", login);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return false;
        return Convert.ToInt32(result) > 0;
    }

    private static UserLogin MapToUserLogin(MySqlDataReader reader)
    {
        var idOrdinal = reader.GetOrdinal("id");
        var userIdOrdinal = reader.GetOrdinal("id_operatora");
        var loginOrdinal = reader.GetOrdinal("login");
        var passwordHashOrdinal = reader.GetOrdinal("haslohash");

        if (reader.IsDBNull(idOrdinal))
            throw new InvalidOperationException("Pole 'id' nie może być NULL w tabeli operator_login.");
        if (reader.IsDBNull(userIdOrdinal))
            throw new InvalidOperationException("Pole 'id_operatora' nie może być NULL w tabeli operator_login.");
        if (reader.IsDBNull(loginOrdinal))
            throw new InvalidOperationException("Pole 'login' nie może być NULL w tabeli operator_login.");
        if (reader.IsDBNull(passwordHashOrdinal))
            throw new InvalidOperationException("Pole 'haslohash' nie może być NULL w tabeli operator_login.");

        var id = reader.GetInt32(idOrdinal);
        var userId = reader.GetInt32(userIdOrdinal);
        var login = reader.GetString(loginOrdinal);
        var passwordHash = reader.GetString(passwordHashOrdinal);

        var userLogin = new UserLogin(userId, login, passwordHash);
        
        // Ustawiamy Id używając refleksji
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        idProperty.SetValue(userLogin, id);

        return userLogin;
    }
}
