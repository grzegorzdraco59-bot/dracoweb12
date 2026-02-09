namespace ERP.Infrastructure.Services;

/// <summary>
/// Dostarcza connection string na podstawie ustawienia ActiveDatabase.
/// Umożliwia ręczny wybór środowiska: LOCBD | DRACO_OFFICE_WIFI | DRACO_REMOTE.
/// </summary>
public interface IConnectionStringProvider
{
    /// <summary>Zwraca connection string dla aktualnie wybranego środowiska.</summary>
    string GetConnectionString();

    /// <summary>Zwraca aktualną wartość ActiveDatabase (LOCBD | DRACO_OFFICE_WIFI | DRACO_REMOTE).</summary>
    string GetActiveDatabase();

    /// <summary>Zapisuje wybór użytkownika do pliku lokalnego. Zmiana obowiązuje od następnego połączenia.</summary>
    void SetActiveDatabase(string value);

    /// <summary>Zwraca czytelną nazwę środowiska (np. "Test (locbd)").</summary>
    string GetEnvironmentDisplayName();

    /// <summary>Testuje połączenie z bazą (SELECT 1). Zwraca null gdy OK, komunikat błędu w przeciwnym razie.</summary>
    Task<string?> TestConnectionAsync();

    /// <summary>Zwraca Server, Port, Database (bez Uid/Pwd) do wyświetlenia w teście połączenia.</summary>
    (string Server, int Port, string Database) GetConnectionInfoForDisplay();
}
