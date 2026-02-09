using System.Diagnostics;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Atomowe generowanie ID dla tabel bez AUTO_INCREMENT (Clarion).
/// Używa tabeli id_sequences i LAST_INSERT_ID; brak MAX(id)+1.
/// </summary>
public class IdGeneratorService : IIdGenerator
{
    private const string InsertOrUpdateSequence =
        "INSERT INTO id_sequences (table_name, next_id) VALUES (@tableName, 1) " +
        "ON DUPLICATE KEY UPDATE next_id = LAST_INSERT_ID(next_id + 1);";
    private const string SelectLastInsertId = "SELECT LAST_INSERT_ID();";

    public IdGeneratorService()
    {
    }

    public async Task<long> GetNextIdAsync(string tableName, MySqlConnection conn, MySqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("tableName nie może być puste.", nameof(tableName));

        var table = tableName.Trim();

        if (string.Equals(table, "faktury", StringComparison.OrdinalIgnoreCase))
            await EnsureFakturySequenceAheadAsync(conn, transaction, cancellationToken).ConfigureAwait(false);
        if (string.Equals(table, "pozycjefaktury", StringComparison.OrdinalIgnoreCase))
            await EnsurePozycjeFakturySequenceAheadAsync(conn, transaction, cancellationToken).ConfigureAwait(false);

        var upsert = new MySqlCommand(InsertOrUpdateSequence, conn, transaction);
        upsert.Parameters.AddWithValue("@tableName", table);
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        var select = new MySqlCommand(SelectLastInsertId, conn, transaction);
        var result = await select.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException($"id_sequences nie zwróciło wartości dla '{table}'.");

        var newId = Convert.ToInt64(result);
        Debug.WriteLine($"[IdGenerator] table={table}, issued_id={newId}");
        return newId;
    }

    private static async Task EnsureFakturySequenceAheadAsync(MySqlConnection conn, MySqlTransaction transaction, CancellationToken cancellationToken)
    {
        var ensureRow = new MySqlCommand(
            "INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('faktury', 1);",
            conn, transaction);
        await ensureRow.ExecuteNonQueryAsync(cancellationToken);

        var getNext = new MySqlCommand(
            "SELECT next_id FROM id_sequences WHERE table_name = 'faktury' FOR UPDATE;",
            conn, transaction);
        var nextResult = await getNext.ExecuteScalarAsync(cancellationToken);
        var nextId = nextResult == null || nextResult == DBNull.Value ? 0L : Convert.ToInt64(nextResult);

        var getMax = new MySqlCommand(
            "SELECT COALESCE(MAX(id_faktury), 0) FROM faktury;",
            conn, transaction);
        var maxResult = await getMax.ExecuteScalarAsync(cancellationToken);
        var maxId = maxResult == null || maxResult == DBNull.Value ? 0L : Convert.ToInt64(maxResult);

        if (nextId <= maxId)
        {
            var bump = new MySqlCommand(
                "UPDATE id_sequences SET next_id = @NextId WHERE table_name = 'faktury';",
                conn, transaction);
            bump.Parameters.AddWithValue("@NextId", maxId);
            await bump.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task EnsurePozycjeFakturySequenceAheadAsync(MySqlConnection conn, MySqlTransaction transaction, CancellationToken cancellationToken)
    {
        var ensureRow = new MySqlCommand(
            "INSERT IGNORE INTO id_sequences (table_name, next_id) VALUES ('pozycjefaktury', 1);",
            conn, transaction);
        await ensureRow.ExecuteNonQueryAsync(cancellationToken);

        var getNext = new MySqlCommand(
            "SELECT next_id FROM id_sequences WHERE table_name = 'pozycjefaktury' FOR UPDATE;",
            conn, transaction);
        var nextResult = await getNext.ExecuteScalarAsync(cancellationToken);
        var nextId = nextResult == null || nextResult == DBNull.Value ? 0L : Convert.ToInt64(nextResult);

        var getMax = new MySqlCommand(
            "SELECT COALESCE(MAX(id_pozycji_faktury), 0) FROM pozycjefaktury;",
            conn, transaction);
        var maxResult = await getMax.ExecuteScalarAsync(cancellationToken);
        var maxId = maxResult == null || maxResult == DBNull.Value ? 0L : Convert.ToInt64(maxResult);

        if (nextId <= maxId)
        {
            var bump = new MySqlCommand(
                "UPDATE id_sequences SET next_id = @NextId WHERE table_name = 'pozycjefaktury';",
                conn, transaction);
            bump.Parameters.AddWithValue("@NextId", maxId);
            await bump.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
