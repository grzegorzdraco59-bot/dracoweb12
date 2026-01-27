using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium User (operator) używająca MySqlConnector
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _context;

    public UserRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_operatora, id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse " +
            "FROM operator WHERE id_operatora = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToUser(reader);
        }

        return null;
    }

    public async Task<User?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_operatora, id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse " +
            "FROM operator WHERE imie_nazwisko = @FullName LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@FullName", fullName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToUser(reader);
        }

        return null;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<User>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_operatora, id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse " +
            "FROM operator ORDER BY imie_nazwisko",
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(MapToUser(reader));
        }

        return users;
    }

    public async Task<IEnumerable<User>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var users = new List<User>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_operatora, id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse " +
            "FROM operator WHERE id_firmy = @CompanyId ORDER BY imie_nazwisko",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(MapToUser(reader));
        }

        return users;
    }

    public async Task<int> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO operator (id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse) " +
            "VALUES (@CompanyId, @FullName, @Permissions, @SenderEmail, @SenderUserName, " +
            "@SenderEmailServer, @SenderEmailPassword, @MessageText, @CcAddress); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        AddUserParameters(command, user);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE operator SET id_firmy = @CompanyId, imie_nazwisko = @FullName, uprawnienia = @Permissions, " +
            "senderEmail = @SenderEmail, senderUserName = @SenderUserName, senderEmailServer = @SenderEmailServer, " +
            "senderEmailPassword = @SenderEmailPassword, messageText = @MessageText, ccAdresse = @CcAddress " +
            "WHERE id_operatora = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", user.Id);
        AddUserParameters(command, user);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("SELECT COUNT(1) FROM operator WHERE id_operatora = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static User MapToUser(MySqlDataReader reader)
    {
        // Używamy refleksji do ustawienia Id i innych właściwości
        var id = reader.GetInt32(reader.GetOrdinal("id_operatora"));
        var companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        var fullName = reader.GetString(reader.GetOrdinal("imie_nazwisko"));
        var permissions = reader.GetInt32(reader.GetOrdinal("uprawnienia"));
        
        var senderEmail = reader.IsDBNull(reader.GetOrdinal("senderEmail")) ? string.Empty : reader.GetString(reader.GetOrdinal("senderEmail"));
        var senderUserName = reader.IsDBNull(reader.GetOrdinal("senderUserName")) ? string.Empty : reader.GetString(reader.GetOrdinal("senderUserName"));
        var senderEmailServer = reader.IsDBNull(reader.GetOrdinal("senderEmailServer")) ? string.Empty : reader.GetString(reader.GetOrdinal("senderEmailServer"));
        var senderEmailPassword = reader.IsDBNull(reader.GetOrdinal("senderEmailPassword")) ? string.Empty : reader.GetString(reader.GetOrdinal("senderEmailPassword"));
        var messageText = reader.IsDBNull(reader.GetOrdinal("messageText")) ? string.Empty : reader.GetString(reader.GetOrdinal("messageText"));
        var ccAddress = reader.IsDBNull(reader.GetOrdinal("ccAdresse")) ? string.Empty : reader.GetString(reader.GetOrdinal("ccAdresse"));

        var user = new User(companyId, fullName, permissions);
        user.UpdateEmailSettings(senderEmail, senderUserName, senderEmailServer, senderEmailPassword);
        
        // Ustawiamy Id używając refleksji
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        idProperty.SetValue(user, id);

        return user;
    }

    private static void AddUserParameters(MySqlCommand command, User user)
    {
        command.Parameters.AddWithValue("@CompanyId", user.DefaultCompanyId);
        command.Parameters.AddWithValue("@FullName", user.FullName);
        command.Parameters.AddWithValue("@Permissions", user.Permissions);
        command.Parameters.AddWithValue("@SenderEmail", user.SenderEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SenderUserName", user.SenderUserName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SenderEmailServer", user.SenderEmailServer ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SenderEmailPassword", user.SenderEmailPassword ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@MessageText", user.MessageText ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CcAddress", user.CcAddress ?? (object)DBNull.Value);
    }
}
