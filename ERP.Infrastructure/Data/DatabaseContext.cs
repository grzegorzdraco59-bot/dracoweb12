using MySqlConnector;
using ERP.Shared.Constants;

namespace ERP.Infrastructure.Data;

/// <summary>
/// Główny kontekst dostępu do bazy danych
/// </summary>
public class DatabaseContext
{
    private readonly string _connectionString;

    public DatabaseContext(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    // Konstruktor bez parametrów został usunięty - connection string musi być zawsze przekazany
    // aby uniknąć hardcoded wartości w kodzie

    public async Task<MySqlConnection> CreateConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        return connection;
    }

    public MySqlConnection CreateConnection()
    {
        var connection = new MySqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}