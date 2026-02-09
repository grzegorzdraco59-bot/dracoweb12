using MySqlConnector;
using ERP.Infrastructure.Services;

namespace ERP.Infrastructure.Data;

/// <summary>
/// Główny kontekst dostępu do bazy danych.
/// Pobiera connection string wyłącznie przez IConnectionStringProvider.
/// Każde CreateConnection zwraca NOWE połączenie – brak cache.
/// </summary>
public class DatabaseContext
{
    /// <summary>Tymczasowa diagnostyka: pierwsze połączenie – podaj handler aby pokazać MessageBox (np. w WPF).</summary>
    public static Action<string>? OnFirstConnectionDiagnostic;

    private readonly IConnectionStringProvider _provider;
    private static int _connectionCount;
    private const int MaxDiagnosticDisplays = 3;

    public DatabaseContext(IConnectionStringProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public async Task<MySqlConnection> CreateConnectionAsync()
    {
        var connectionString = _provider.GetConnectionString();
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        if (connection == null)
            throw new InvalidOperationException("DatabaseContext returned null connection.");
        LogConnectionDiagnostic();
        return connection;
    }

    public MySqlConnection CreateConnection()
    {
        var connectionString = _provider.GetConnectionString();
        var connection = new MySqlConnection(connectionString);
        connection.Open();
        LogConnectionDiagnostic();
        return connection;
    }

    /// <summary>Diagnostyka (tymczasowa): pierwsze N połączeń – log + MessageBox.</summary>
    private void LogConnectionDiagnostic()
    {
        var n = System.Threading.Interlocked.Increment(ref _connectionCount);
        if (n > MaxDiagnosticDisplays) return;
        try
        {
            var active = _provider.GetActiveDatabase();
            var (server, port, database) = _provider.GetConnectionInfoForDisplay();
            var msg = $"DB operacja #{n}: ActiveDatabase={active}, Server={server}, Port={port}, Database={database}";
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "db_active.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\r\n");
            if (n == 1)
                OnFirstConnectionDiagnostic?.Invoke(msg);
        }
        catch { /* ignoruj błędy diagnostyki */ }
    }
}