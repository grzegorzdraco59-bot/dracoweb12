using System.Text;
using System.Text.RegularExpressions;
#if WINDOWS
using System.Windows;
#endif
using MySqlConnector;

namespace ERP.Infrastructure.Data;

/// <summary>
/// Rozszerzenia MySqlCommand z diagnostyką SQL (tymczasowe).
/// </summary>
internal static class MySqlCommandExtensions
{
    // DEBUG SQL - tymczasowa diagnostyka dla MySqlException "Unknown column"

    /// <summary>
    /// Waliduje, że wszystkie parametry @X z SQL są zdefiniowane w cmd.Parameters.
    /// Rzuca InvalidOperationException z listą brakujących parametrów, jeśli są.
    /// </summary>
    private static void ValidateParametersOrThrow(MySqlCommand command)
    {
        var sql = command.CommandText ?? "";
        var matches = Regex.Matches(sql, @"@([a-zA-Z_][a-zA-Z0-9_]*)");
        var requiredParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in matches.Cast<Match>())
            requiredParams.Add("@" + m.Groups[1].Value);

        var existingParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (MySqlParameter p in command.Parameters)
        {
            var name = p.ParameterName ?? "";
            existingParams.Add(name.StartsWith("@") ? name : "@" + name);
        }

        var missing = requiredParams.Where(r => !existingParams.Contains(r)).OrderBy(x => x).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Brakujące parametry w MySqlCommand: {string.Join(", ", missing)}\n\nSQL:\n{sql}");
        }
    }

    public static async Task<MySqlDataReader> ExecuteReaderWithDiagnosticsAsync(
        this MySqlCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateParametersOrThrow(command);
        try
        {
            return await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.Message.Contains("Unknown column"))
        {
            // DEBUG SQL
            ShowDiagnostics(ex, command);
            throw;
        }
    }

    public static async Task<object?> ExecuteScalarWithDiagnosticsAsync(
        this MySqlCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateParametersOrThrow(command);
        try
        {
            return await command.ExecuteScalarAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.Message.Contains("Unknown column"))
        {
            // DEBUG SQL
            ShowDiagnostics(ex, command);
            throw;
        }
    }

    public static async Task<int> ExecuteNonQueryWithDiagnosticsAsync(
        this MySqlCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateParametersOrThrow(command);
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException ex) when (ex.Message.Contains("Unknown column"))
        {
            // DEBUG SQL
            ShowDiagnostics(ex, command);
            throw;
        }
    }

    /// <summary>
    /// Wykonuje polecenie SQL zawierające INSERT oraz SELECT LAST_INSERT_ID(); w jednej transakcji.
    /// Zwraca nowe ID (AUTO_INCREMENT) z serwera. NIE generuje ID lokalnie.
    /// Wymagane: command.CommandText musi kończyć się na "; SELECT LAST_INSERT_ID();"
    /// Wzorzec Clarion "Insert record": INSERT bez ID → pobierz LAST_INSERT_ID() → użyj newId do UPDATE/edycji.
    /// </summary>
    public static async Task<long> ExecuteInsertAndGetIdAsync(
        this MySqlCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Connection == null)
            throw new InvalidOperationException("Command must have a connection.");

        await using var transaction = await command.Connection.BeginTransactionAsync(cancellationToken);
        command.Transaction = transaction;
        try
        {
            var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Convert.ToInt64(result ?? 0);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ShowDiagnostics(MySqlException ex, MySqlCommand cmd)
    {
        var sb = new StringBuilder();
        foreach (MySqlParameter p in cmd.Parameters)
        {
            sb.AppendLine($"{p.ParameterName} = {p.Value}");
        }
#if WINDOWS
        MessageBox.Show(
            $"{ex.Message}\n\nSQL:\n{cmd.CommandText}\n\nPARAMS:\n{sb}",
            "DEBUG SQL - Unknown column",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
#endif
    }
}
