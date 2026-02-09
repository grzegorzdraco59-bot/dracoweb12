using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Provider connection stringów na podstawie ActiveDatabase.
/// Fallback do LOCBD przy nieznanej wartości lub braku konfiguracji.
/// </summary>
public class ConnectionStringProvider : IConnectionStringProvider
{
    private const string LocBd = "LOCBD";
    private const string DracoOfficeWifi = "DRACO_OFFICE_WIFI";
    private const string DracoRemote = "DRACO_REMOTE";

    private static readonly string[] AllowedValues = [LocBd, DracoOfficeWifi, DracoRemote];

    private readonly IConfiguration _config;
    private readonly string _settingsFilePath;

    public ConnectionStringProvider(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "database_settings.json");
    }

    private const string DracoBd = "DRACO_BD";

    public string GetConnectionString()
    {
        var active = GetActiveDatabase();
        var key = active switch
        {
            LocBd => "LocBD",
            DracoOfficeWifi => "DracoOfficeWifi",
            DracoRemote => "DracoRemote",
            _ => "LocBD"
        };
        var cs = _config.GetConnectionString(key)
            ?? _config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException($"Brak connection string dla '{key}'. Skonfiguruj ConnectionStrings:{key} w appsettings.json.");
        ValidateDatabaseName(active, cs);
        return cs;
    }

    private static void ValidateDatabaseName(string active, string connectionString)
    {
        if (active is not DracoOfficeWifi and not DracoRemote)
            return;
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var db = (builder.Database ?? "").Trim();
        if (!db.Equals(DracoBd, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "BŁĄD KONFIGURACJI: dla środowiska " + active + " baza musi być DRACO_BD. Obecnie: " + (string.IsNullOrEmpty(db) ? "(pusta)" : db));
    }

    public (string Server, int Port, string Database) GetConnectionInfoForDisplay()
    {
        var cs = GetConnectionString();
        var builder = new MySqlConnectionStringBuilder(cs);
        return (builder.Server ?? "", builder.Port > 0 ? (int)builder.Port : 3306, builder.Database ?? "");
    }

    public string GetActiveDatabase()
    {
        // 1. Plik użytkownika (najwyższy priorytet)
        var fromFile = ReadFromUserFile();
        if (!string.IsNullOrWhiteSpace(fromFile) && IsAllowed(fromFile))
            return Normalize(fromFile);

        // 2. appsettings.json
        var fromConfig = _config["ActiveDatabase"] ?? _config["Database:ActiveDatabase"];
        if (!string.IsNullOrWhiteSpace(fromConfig) && IsAllowed(fromConfig))
            return Normalize(fromConfig);

        return LocBd;
    }

    public void SetActiveDatabase(string value)
    {
        var normalized = Normalize(value);
        if (!IsAllowed(normalized))
            throw new ArgumentException($"Dozwolone wartości: {string.Join(", ", AllowedValues)}", nameof(value));

        var dir = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(new { ActiveDatabase = normalized });
        File.WriteAllText(_settingsFilePath, json);
    }

    public string GetEnvironmentDisplayName()
    {
        return GetActiveDatabase() switch
        {
            LocBd => "Test (locbd)",
            DracoOfficeWifi => "Biuro (Wi-Fi)",
            DracoRemote => "Zdalnie (Internet/VPN)",
            _ => "Test (locbd)"
        };
    }

    public async Task<string?> TestConnectionAsync()
    {
        try
        {
            var cs = GetConnectionString();
            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();
            return null; // sukces
        }
        catch (MySqlException ex)
        {
            return FormatMySqlError(ex);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private string? ReadFromUserFile()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return null;
            var json = File.ReadAllText(_settingsFilePath);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("ActiveDatabase", out var prop)
                ? prop.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string Normalize(string value) =>
        (value ?? "").Trim().ToUpperInvariant();

    private static bool IsAllowed(string value) =>
        AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static string FormatMySqlError(MySqlException ex)
    {
        return ex.Number switch
        {
            0 => "Nie można połączyć z serwerem. Sprawdź adres, port, sieć (VPN).",
            1042 => "Timeout połączenia. Serwer niedostępny lub blokada firewall.",
            1045 => "Błąd autoryzacji (login/hasło).",
            1049 => "Baza danych nie istnieje.",
            _ => $"{ex.Message} (kod: {ex.Number})"
        };
    }
}
