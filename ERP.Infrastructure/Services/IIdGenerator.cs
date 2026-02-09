using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Atomowe generowanie ID dla tabel bez AUTO_INCREMENT (Clarion).
/// Wymaga połączenia i transakcji – używać w tej samej transakcji co INSERT.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Pobiera następne ID dla tabeli. Atomowe (FOR UPDATE + blokada).
    /// </summary>
    /// <param name="tableName">Nazwa tabeli, np. "aoferty", "apozycjeoferty"</param>
    /// <param name="conn">Połączenie (musi być w transakcji)</param>
    /// <param name="transaction">Transakcja – ta sama co dla INSERT</param>
    Task<long> GetNextIdAsync(string tableName, MySqlConnection conn, MySqlTransaction transaction, CancellationToken cancellationToken = default);
}
