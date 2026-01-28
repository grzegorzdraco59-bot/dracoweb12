using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Application.Services;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium OrderMain (zamowienia) używająca MySqlConnector
/// </summary>
public class OrderMainRepository : IOrderMainRepository
{
    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;

    public OrderMainRepository(DatabaseContext context, IUserContext userContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    private int GetCurrentCompanyId()
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
        return companyId.Value;
    }

    public async Task<OrderMainDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        // Użyjmy dynamicznego sprawdzania nazwy kolumny ID
        var idColumnName = await GetIdColumnNameAsync(connection, cancellationToken);
        var command = new MySqlCommand(
            $"SELECT * FROM zamowienia WHERE {idColumnName} = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToDto(reader);
        }

        return null;
    }

    public async Task<IEnumerable<OrderMainDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await GetByCompanyIdAsync(GetCurrentCompanyId(), cancellationToken);
    }

    public async Task<IEnumerable<OrderMainDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var orders = new List<OrderMainDto>();
        try
        {
            await using var connection = await _context.CreateConnectionAsync();
            var command = new MySqlCommand(
                "SELECT * FROM zamowienia WHERE id_firmy = @CompanyId ORDER BY data_zamowienia DESC, nr_zamowienia DESC",
                connection);
            command.Parameters.AddWithValue("@CompanyId", companyId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    orders.Add(MapToDto(reader));
                }
                catch (Exception ex)
                {
                    // Loguj błąd mapowania, ale kontynuuj z następnym rekordem
                    System.Diagnostics.Debug.WriteLine($"Błąd mapowania zamówienia: {ex.Message}");
                }
            }
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1146)
        {
            // Tabela nie istnieje
            System.Diagnostics.Debug.WriteLine($"Tabela 'zamowienia' nie istnieje: {ex.Message}");
            throw new InvalidOperationException($"Tabela 'zamowienia' nie istnieje w bazie danych. Sprawdź czy tabela została utworzona.", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd podczas ładowania zamówień: {ex.Message}");
            throw;
        }

        return orders;
    }

    public async Task<int> AddAsync(OrderMainDto order, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        // Konwersja daty do formatu Clarion
        int? orderDateInt = null;
        if (order.OrderDate.HasValue)
        {
            var baseDate = new DateTime(1800, 12, 28);
            orderDateInt = (int)(order.OrderDate.Value - baseDate).TotalDays;
        }
        
        var command = new MySqlCommand(
            "INSERT INTO zamowienia (id_firmy, nr_zamowienia, data_zamowienia, id_dostawcy, dostawca, uwagi, status) " +
            "VALUES (@CompanyId, @OrderNumber, @OrderDateInt, @SupplierId, @SupplierName, @Notes, @Status); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@CompanyId", order.CompanyId);
        command.Parameters.AddWithValue("@OrderNumber", order.OrderNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderDateInt", orderDateInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierId", order.SupplierId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierName", order.SupplierName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", order.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Status", order.Status ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(OrderMainDto order, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var idColumnName = await GetIdColumnNameAsync(connection, cancellationToken);
        // Konwersja daty do formatu Clarion
        int? orderDateInt = null;
        if (order.OrderDate.HasValue)
        {
            var baseDate = new DateTime(1800, 12, 28);
            orderDateInt = (int)(order.OrderDate.Value - baseDate).TotalDays;
        }
        
        var command = new MySqlCommand(
            "UPDATE zamowienia SET " +
            "nr_zamowienia = @OrderNumber, data_zamowienia = @OrderDateInt, " +
            "id_dostawcy = @SupplierId, dostawca = @SupplierName, " +
            "uwagi = @Notes, status = @Status " +
            $"WHERE {idColumnName} = @Id AND id_firmy = @CompanyId",
            connection);

        command.Parameters.AddWithValue("@Id", order.Id);
        command.Parameters.AddWithValue("@CompanyId", order.CompanyId);
        command.Parameters.AddWithValue("@OrderNumber", order.OrderNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderDateInt", orderDateInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierId", order.SupplierId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierName", order.SupplierName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", order.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Status", order.Status ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        // Użyjmy dynamicznego sprawdzania nazwy kolumny ID
        var idColumnName = await GetIdColumnNameAsync(connection, cancellationToken);
        var command = new MySqlCommand(
            $"DELETE FROM zamowienia WHERE {idColumnName} = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<string> GetIdColumnNameAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var checkCommand = new MySqlCommand("SHOW COLUMNS FROM zamowienia", connection);
            await using var reader = await checkCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var columnName = reader.GetString(0);
                var key = reader.GetString(3);
                if (key == "PRI" && columnName.ToLowerInvariant().Contains("id"))
                {
                    return columnName;
                }
            }
        }
        catch
        {
            // Jeśli nie możemy sprawdzić, użyjemy domyślnej nazwy
        }
        return "id"; // domyślna nazwa
    }

    private static OrderMainDto MapToDto(MySqlDataReader reader)
    {
        // Najpierw sprawdźmy jakie kolumny są dostępne
        var availableColumns = new HashSet<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            availableColumns.Add(reader.GetName(i));
        }

        // Sprawdźmy nazwę kolumny ID - może być id, id_zamowienia, ID_zamowienia itp.
        string? idColumnName = null;
        if (availableColumns.Contains("id"))
            idColumnName = "id";
        else if (availableColumns.Contains("id_zamowienia"))
            idColumnName = "id_zamowienia";
        else if (availableColumns.Contains("ID_zamowienia"))
            idColumnName = "ID_zamowienia";
        else if (availableColumns.Contains("ID"))
            idColumnName = "ID";
        
        // Konwersja daty z formatu Clarion (liczba dni od 1800-12-28)
        DateTime? orderDate = null;
        var orderDateInt = GetNullableInt(reader, availableColumns.Contains("data_zamowienia") ? "data_zamowienia" : null);
        if (orderDateInt.HasValue)
        {
            orderDate = new DateTime(1800, 12, 28).AddDays(orderDateInt.Value);
        }
        
        return new OrderMainDto
        {
            Id = GetInt(reader, idColumnName),
            CompanyId = GetInt(reader, "id_firmy"),
            OrderNumber = GetNullableInt(reader, availableColumns.Contains("nr_zamowienia") ? "nr_zamowienia" : availableColumns.Contains("id_zamowienia") ? "id_zamowienia" : null),
            OrderDate = orderDate,
            SupplierId = GetNullableInt(reader, availableColumns.Contains("id_dostawcy") ? "id_dostawcy" : null),
            SupplierName = GetNullableString(reader, availableColumns.Contains("dostawca") ? "dostawca" : availableColumns.Contains("nazwa_dostawcy") ? "nazwa_dostawcy" : null),
            Notes = GetNullableString(reader, availableColumns.Contains("uwagi") ? "uwagi" : null),
            Status = GetNullableString(reader, availableColumns.Contains("status") ? "status" : null),
            CreatedAt = GetDateTime(reader, availableColumns.Contains("CreatedAt") ? "CreatedAt" : availableColumns.Contains("created_at") ? "created_at" : null),
            UpdatedAt = GetNullableDateTime(reader, availableColumns.Contains("UpdatedAt") ? "UpdatedAt" : availableColumns.Contains("updated_at") ? "updated_at" : null)
        };
    }

    private static int GetInt(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return 0;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }
        catch
        {
            return 0;
        }
    }

    private static int? GetNullableInt(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private static DateTime GetDateTime(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return DateTime.MinValue;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static DateTime? GetNullableDateTime(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetNullableString(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch
        {
            return null;
        }
    }
}
