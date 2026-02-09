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
        if (id <= 0)
            return null;

        await using var connection = await _context.CreateConnectionAsync();
        const string sql = """
            SELECT
              id,
              id_firmy,
              imie_nazwisko,
              uprawnienia,
              senderEmail,
              senderUserName,
              senderEmailServer,
              senderEmailPassword,
              messageText,
              ccAdresse
            FROM `operator`
            WHERE id = @id
            LIMIT 1
            """;
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapToUser(reader);

        return null;
    }

    public async Task<User?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        using var command = new MySqlCommand(
            "SELECT id, id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse " +
            "FROM `operator` WHERE imie_nazwisko = @FullName LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@FullName", fullName);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
        using var command = new MySqlCommand(
            "SELECT id, id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse " +
            "FROM `operator` ORDER BY imie_nazwisko",
            connection);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
        using var command = new MySqlCommand(
            "SELECT id, id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse " +
            "FROM `operator` WHERE id_firmy = @CompanyId ORDER BY imie_nazwisko",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(MapToUser(reader));
        }

        return users;
    }

    public async Task<int> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        using var command = new MySqlCommand(
            "INSERT INTO `operator` (id_firmy, imie_nazwisko, uprawnienia, senderEmail, senderUserName, " +
            "senderEmailServer, senderEmailPassword, messageText, ccAdresse) " +
            "VALUES (@CompanyId, @FullName, @Permissions, @SenderEmail, @SenderUserName, " +
            "@SenderEmailServer, @SenderEmailPassword, @MessageText, @CcAddress); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        AddUserParameters(command, user);
        var newId = await command.ExecuteInsertAndGetIdAsync(cancellationToken);
        if (newId == 0)
            throw new InvalidOperationException("Nie udało się pobrać ID nowo utworzonego rekordu operator.");
        return (int)newId;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        using var command = new MySqlCommand(
            "UPDATE `operator` SET id_firmy = @CompanyId, imie_nazwisko = @FullName, uprawnienia = @Permissions, " +
            "senderEmail = @SenderEmail, senderUserName = @SenderUserName, senderEmailServer = @SenderEmailServer, " +
            "senderEmailPassword = @SenderEmailPassword, messageText = @MessageText, ccAdresse = @CcAddress " +
            "WHERE id = @id",
            connection);

        command.Parameters.Add("@id", MySqlDbType.Int32).Value = user.Id;
        AddUserParameters(command, user);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return false;

        await using var connection = await _context.CreateConnectionAsync();
        using var command = new MySqlCommand("SELECT COUNT(1) FROM `operator` WHERE id = @id", connection);
        command.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
    }

    private static User MapToUser(MySqlDataReader reader)
    {
        var idOrd = reader.GetOrdinal("id");
        var id = reader.GetInt32(idOrd);
        var companyId = SafeGetInt32(reader, "id_firmy", 0);
        var fullName = SafeGetString(reader, "imie_nazwisko", string.Empty);
        var permissions = SafeGetInt32(reader, "uprawnienia", 0);

        var senderEmail = SafeGetString(reader, "senderEmail", string.Empty);
        var senderUserName = SafeGetString(reader, "senderUserName", string.Empty);
        var senderEmailServer = SafeGetString(reader, "senderEmailServer", string.Empty);
        var senderEmailPassword = SafeGetString(reader, "senderEmailPassword", string.Empty);
        var messageText = SafeGetString(reader, "messageText", string.Empty);
        var ccAddress = SafeGetString(reader, "ccAdresse", string.Empty);

        var user = new User(companyId, fullName ?? string.Empty, permissions);
        user.UpdateEmailSettings(senderEmail, senderUserName, senderEmailServer, senderEmailPassword);

        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        idProperty.SetValue(user, id);

        return user;
    }

    private static int SafeGetInt32(MySqlDataReader reader, string column, int defaultValue)
    {
        try
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? defaultValue : reader.GetInt32(ord);
        }
        catch { return defaultValue; }
    }

    private static string SafeGetString(MySqlDataReader reader, string column, string defaultValue)
    {
        try
        {
            var ord = reader.GetOrdinal(column);
            return reader.IsDBNull(ord) ? defaultValue : (reader.GetString(ord) ?? defaultValue);
        }
        catch { return defaultValue; }
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
