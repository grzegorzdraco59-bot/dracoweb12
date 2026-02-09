using ERP.Application.DTOs;
using ERP.Application.Helpers;
using ERP.Application.Repositories;
using ERP.Application.Services;
using ERP.Domain.Enums;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Services;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium OrderMain (zamowienia) używająca MySqlConnector
/// </summary>
public class OrderMainRepository : IOrderMainRepository
{
    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;
    private readonly IIdGenerator _idGenerator;

    public OrderMainRepository(DatabaseContext context, IUserContext userContext, IIdGenerator idGenerator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
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

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "SELECT z.* FROM zamowienia_V z " +
            "WHERE COALESCE(z.company_id, z.id_firmy) = @CompanyId " +
            "ORDER BY z.data_zamowienia DESC, z.nr_zamowienia DESC, COALESCE(z.id, z.id_zamowienia) DESC",
            connection);
            command.Parameters.AddWithValue("@CompanyId", companyId);

            await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            System.Diagnostics.Debug.WriteLine($"Widok 'zamowienia_V' lub tabela 'zamowienia' nie istnieje: {ex.Message}");
            throw new InvalidOperationException($"Widok zamowienia_V lub tabela zamowienia nie istnieje. Uruchom skrypt 030_CreateZamowienia_V.sql.", ex);
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
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var newId = await _idGenerator.GetNextIdAsync("zamowienia", connection, transaction, cancellationToken);

        var orderDateInt = ClarionDateConverter.DateToClarionInt(order.OrderDate);
        var dataDostawyInt = ClarionDateConverter.DateToClarionInt(order.DataDostawy);
        var dataPlatnosciInt = ClarionDateConverter.DateToClarionInt(order.DataPlatnosci);
        var dataFakturyInt = ClarionDateConverter.DateToClarionInt(order.DataFaktury);

        var command = new MySqlCommand(
            "INSERT INTO zamowienia (id_zamowienia, id_firmy, nr_zamowienia, data_zamowienia, id_dostawcy, dostawca, dostawca_mail, " +
            "waluta, tabela_nbp, kurs_waluty, data_dostawy, nr_faktury, status_platnosci, wartosc, status_zamowienia, dla_kogo, " +
            "data_platnosci, uwagi_zam, data_faktury, data_tabeli_nbp, skopiowano_niedostarczone, skopiowano_do_magazynu) " +
            "VALUES (@Id, @CompanyId, @OrderNumber, @OrderDateInt, @SupplierId, @SupplierName, @SupplierEmail, @Waluta, @TabelaNbp, " +
            "@Kurs, @DataDostawyInt, @NrFaktury, @StatusPlatnosci, @Wartosc, @StatusZam, @DlaKogo, @DataPlatnosciInt, @Uwagi, " +
            "@DataFakturyInt, @DataTabeliNbp, @SkopNiedost, @SkopMagazyn)",
            connection, transaction);

        command.Parameters.AddWithValue("@Id", newId);
        command.Parameters.AddWithValue("@CompanyId", order.CompanyId);
        command.Parameters.AddWithValue("@OrderNumber", order.OrderNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderDateInt", orderDateInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierId", order.SupplierId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierName", order.SupplierName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierEmail", order.SupplierEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Waluta", order.Waluta ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TabelaNbp", order.TabelaNbp ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Kurs", order.Kurs ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataDostawyInt", dataDostawyInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@NrFaktury", order.NrFaktury ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StatusPlatnosci", order.StatusPlatnosci ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Wartosc", order.Wartosc ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StatusZam", order.StatusZamowienia ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DlaKogo", order.DlaKogo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataPlatnosciInt", dataPlatnosciInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Uwagi", order.Notes ?? order.Uwagi ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataFakturyInt", dataFakturyInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataTabeliNbp", order.DataTabeliNbp ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SkopNiedost", order.SkopiowanoNiedostarczone ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SkopMagazyn", order.SkopiowanoDoMagazynu ?? (object)DBNull.Value);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (int)newId;
    }

    public async Task UpdateAsync(OrderMainDto order, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var idColumnName = await GetIdColumnNameAsync(connection, cancellationToken);
        var orderDateInt = ClarionDateConverter.DateToClarionInt(order.OrderDate);
        var dataDostawyInt = ClarionDateConverter.DateToClarionInt(order.DataDostawy);
        var dataPlatnosciInt = ClarionDateConverter.DateToClarionInt(order.DataPlatnosci);
        var dataFakturyInt = ClarionDateConverter.DateToClarionInt(order.DataFaktury);

        var command = new MySqlCommand(
            "UPDATE zamowienia SET " +
            "nr_zamowienia = @OrderNumber, data_zamowienia = @OrderDateInt, id_dostawcy = @SupplierId, dostawca = @SupplierName, " +
            "dostawca_mail = @SupplierEmail, waluta = @Waluta, tabela_nbp = @TabelaNbp, kurs_waluty = @Kurs, " +
            "data_dostawy = @DataDostawyInt, nr_faktury = @NrFaktury, status_platnosci = @StatusPlatnosci, wartosc = @Wartosc, " +
            "status_zamowienia = @StatusZam, dla_kogo = @DlaKogo, data_platnosci = @DataPlatnosciInt, uwagi_zam = @Uwagi, " +
            "data_faktury = @DataFakturyInt, data_tabeli_nbp = @DataTabeliNbp, " +
            "skopiowano_niedostarczone = @SkopNiedost, skopiowano_do_magazynu = @SkopMagazyn " +
            $"WHERE {idColumnName} = @Id AND id_firmy = @CompanyId",
            connection);

        command.Parameters.AddWithValue("@Id", order.Id);
        command.Parameters.AddWithValue("@CompanyId", order.CompanyId);
        command.Parameters.AddWithValue("@OrderNumber", order.OrderNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderDateInt", orderDateInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierId", order.SupplierId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierName", order.SupplierName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierEmail", order.SupplierEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Waluta", order.Waluta ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TabelaNbp", order.TabelaNbp ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Kurs", order.Kurs ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataDostawyInt", dataDostawyInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@NrFaktury", order.NrFaktury ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StatusPlatnosci", order.StatusPlatnosci ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Wartosc", order.Wartosc ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StatusZam", order.StatusZamowienia ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DlaKogo", order.DlaKogo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataPlatnosciInt", dataPlatnosciInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Uwagi", order.Notes ?? order.Uwagi ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataFakturyInt", dataFakturyInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataTabeliNbp", order.DataTabeliNbp ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SkopNiedost", order.SkopiowanoNiedostarczone ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SkopMagazyn", order.SkopiowanoDoMagazynu ?? (object)DBNull.Value);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var idColumnName = await GetIdColumnNameAsync(connection, cancellationToken);
        var command = new MySqlCommand(
            $"DELETE FROM zamowienia WHERE {idColumnName} = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task SetStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var idColumnName = await GetIdColumnNameAsync(connection, cancellationToken);
        var command = new MySqlCommand(
            $"UPDATE zamowienia SET status = @Status WHERE {idColumnName} = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        command.Parameters.AddWithValue("@Status", OrderStatusMapping.ToDb(status));
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task RecalculateOrderTotalAsync(int orderId, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
            return;
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE zamowienia z " +
            "SET z.wartosc = (" +
            "  SELECT IFNULL(ROUND(SUM(IFNULL(p.ilosc_zamawiana,0) * IFNULL(p.cena_zamawiana,0)), 2), 0) " +
            "  FROM pozycjezamowienia p " +
            "  WHERE p.id_zamowienia = z.id_zamowienia" +
            ") " +
            "WHERE z.id_zamowienia = @OrderId",
            connection);
        command.Parameters.AddWithValue("@OrderId", orderId);
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    private async Task<string> GetIdColumnNameAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var checkCommand = new MySqlCommand("SHOW COLUMNS FROM zamowienia", connection);
            await using var reader = await checkCommand.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
        var availableColumns = new HashSet<string>();
        for (int i = 0; i < reader.FieldCount; i++)
            availableColumns.Add(reader.GetName(i));

        string? idColumnName = availableColumns.Contains("id") ? "id" :
            availableColumns.Contains("id_zamowienia") ? "id_zamowienia" :
            availableColumns.Contains("ID_zamowienia") ? "ID_zamowienia" : "ID";

        var dataZamInt = GetNullableInt(reader, availableColumns.Contains("data_zamowienia") ? "data_zamowienia" : null);
        var dataDostawyInt = GetNullableInt(reader, availableColumns.Contains("data_dostawy") ? "data_dostawy" : null);
        var dataPlatnosciInt = GetNullableInt(reader, availableColumns.Contains("data_platnosci") ? "data_platnosci" : null);
        var dataFakturyInt = GetNullableInt(reader, availableColumns.Contains("data_faktury") ? "data_faktury" : null);

        var dataZam = ClarionDateConverter.ClarionIntToDate(dataZamInt);
        var dataDostawy = ClarionDateConverter.ClarionIntToDate(dataDostawyInt);
        var dataPlatnosci = ClarionDateConverter.ClarionIntToDate(dataPlatnosciInt);
        var dataFaktury = ClarionDateConverter.ClarionIntToDate(dataFakturyInt);

        var dostawca = GetNullableString(reader, availableColumns.Contains("dostawca") ? "dostawca" : availableColumns.Contains("dostawca_nazwa") ? "dostawca_nazwa" : null);
        var statusZam = GetNullableString(reader, availableColumns.Contains("status_zamowienia") ? "status_zamowienia" : availableColumns.Contains("status") ? "status" : null);
        var uwagi = GetNullableString(reader, availableColumns.Contains("uwagi") ? "uwagi" : availableColumns.Contains("uwagi_zam") ? "uwagi_zam" : null);

        var idVal = availableColumns.Contains("id_zamowienia")
            ? GetInt(reader, "id_zamowienia")
            : GetInt(reader, idColumnName);

        return new OrderMainDto
        {
            Id = idVal,
            CompanyId = GetInt(reader, availableColumns.Contains("company_id") ? "company_id" : "id_firmy"),
            OrderNumber = GetNullableInt(reader, availableColumns.Contains("nr_zamowienia") ? "nr_zamowienia" : null),
            OrderDate = dataZam,
            SupplierId = GetNullableInt(reader, availableColumns.Contains("id_dostawcy") ? "id_dostawcy" : null),
            SupplierName = dostawca,
            Notes = uwagi,
            Status = OrderStatusMapping.ToDb(OrderStatusMapping.FromDb(statusZam)),
            CreatedAt = DateTime.MinValue,
            UpdatedAt = null,
            IdZamowienia = GetNullableInt(reader, availableColumns.Contains("id_zamowienia") ? "id_zamowienia" : null),
            DataZamowienia = dataZam,
            Nr = GetNullableInt(reader, availableColumns.Contains("nr_zamowienia") ? "nr_zamowienia" : null),
            StatusSkrot = statusZam,
            Skrot = GetNullableString(reader, availableColumns.Contains("skrot") ? "skrot" : null),
            DostawcaNazwa = dostawca,
            Waluta = GetNullableString(reader, availableColumns.Contains("waluta") ? "waluta" : null),
            DataDostawy = dataDostawy,
            StatusZamowienia = statusZam,
            StatusPlatnosci = GetNullableString(reader, availableColumns.Contains("status_platnosci") ? "status_platnosci" : null),
            DataPlatnosci = dataPlatnosci,
            NrFaktury = GetNullableString(reader, availableColumns.Contains("nr_faktury") ? "nr_faktury" : null),
            DataFaktury = dataFaktury,
            Wartosc = GetNullableDecimal(reader, availableColumns.Contains("wartosc") ? "wartosc" : null),
            Uwagi = uwagi,
            DlaKogo = GetNullableString(reader, availableColumns.Contains("dla_kogo") ? "dla_kogo" : null),
            TabelaNbp = GetNullableString(reader, availableColumns.Contains("tabela_nbp") ? "tabela_nbp" : null),
            Kurs = GetNullableDecimal(reader, availableColumns.Contains("kurs_waluty") ? "kurs_waluty" : availableColumns.Contains("kurs") ? "kurs" : null),
            SupplierEmail = GetNullableString(reader, availableColumns.Contains("dostawca_mail") ? "dostawca_mail" : null),
            DataTabeliNbp = GetNullableString(reader, availableColumns.Contains("data_tabeli_nbp") ? "data_tabeli_nbp" : null),
            SkopiowanoNiedostarczone = GetNullableBool(reader, availableColumns.Contains("skopiowano_niedostarczone") ? "skopiowano_niedostarczone" : null),
            SkopiowanoDoMagazynu = GetNullableBool(reader, availableColumns.Contains("skopiowano_do_magazynu") ? "skopiowano_do_magazynu" : null)
        };
    }

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }
        catch { return null; }
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

    private static bool? GetNullableBool(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;
            var val = reader.GetValue(ordinal);
            if (val is bool b) return b;
            if (val is byte by) return by != 0;
            if (val is int i) return i != 0;
            return Convert.ToBoolean(val);
        }
        catch
        {
            return null;
        }
    }
}
