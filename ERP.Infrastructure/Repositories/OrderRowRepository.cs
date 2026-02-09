using ERP.Application.DTOs;
using ERP.Application.Helpers;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Repozytorium odczytu wierszy zamówień z widoku zamowienia_V.
/// </summary>
public class OrderRowRepository : IOrderRowRepository
{
    private readonly DatabaseContext _context;

    public OrderRowRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<OrderRow>> GetByCompanyIdAsync(int companyId, string? searchText = null, CancellationToken cancellationToken = default)
    {
        var rows = new List<OrderRow>();
        await using var connection = await _context.CreateConnectionAsync();
        var sql = "SELECT z.*, " +
                  "COALESCE(z.id, z.id_zamowienia, t.id_zamowienia) AS id, " +
                  "COALESCE(z.company_id, z.id_firmy, t.id_firmy) AS company_id, " +
                  "COALESCE(z.data_zamowienia, t.data_zamowienia) AS data_zamowienia, " +
                  "COALESCE(z.data_platnosci, t.data_platnosci) AS data_platnosci, " +
                  "COALESCE(z.data_faktury, t.data_faktury) AS data_faktury " +
                  "FROM zamowienia_V z " +
                  "LEFT JOIN zamowienia t " +
                  "ON t.id_zamowienia = COALESCE(z.id, z.id_zamowienia) " +
                  "AND t.id_firmy = COALESCE(z.company_id, z.id_firmy) " +
                  "WHERE COALESCE(z.company_id, z.id_firmy) = @CompanyId";
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            sql += " AND (" +
                   "CAST(COALESCE(z.id, z.id_zamowienia, t.id_zamowienia) AS CHAR) LIKE @q OR " +
                   "CAST(COALESCE(z.data_zamowienia, t.data_zamowienia) AS CHAR) LIKE @q OR " +
                   "CAST(z.nr_zamowienia AS CHAR) LIKE @q OR " +
                   "z.dostawca LIKE @q OR " +
                   "z.waluta LIKE @q OR " +
                   "CAST(z.data_dostawy AS CHAR) LIKE @q OR " +
                   "z.status_zamowienia LIKE @q OR " +
                   "z.status_platnosci LIKE @q OR " +
                   "CAST(COALESCE(z.data_platnosci, t.data_platnosci) AS CHAR) LIKE @q OR " +
                   "z.nr_faktury LIKE @q OR " +
                   "CAST(COALESCE(z.data_faktury, t.data_faktury) AS CHAR) LIKE @q OR " +
                   "CAST(z.wartosc AS CHAR) LIKE @q OR " +
                   "z.uwagi LIKE @q OR " +
                   "z.dla_kogo LIKE @q OR " +
                   "z.tabela_nbp LIKE @q OR " +
                   "CAST(z.kurs_waluty AS CHAR) LIKE @q" +
                   ")";
        }
        sql += " ORDER BY data_zamowienia DESC, nr_zamowienia DESC";
        var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        if (!string.IsNullOrWhiteSpace(searchText))
            command.Parameters.AddWithValue("@q", $"%{searchText}%");

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapToRow(reader));
        }

        return rows;
    }

    private static OrderRow MapToRow(MySqlDataReader reader)
    {
        var availableColumns = new HashSet<string>();
        for (int i = 0; i < reader.FieldCount; i++)
            availableColumns.Add(reader.GetName(i));

        string? FindColumn(params string[] names)
        {
            foreach (var name in names)
            {
                if (availableColumns.Contains(name))
                    return name;
            }
            return null;
        }

        var dataZamInt = GetNullableInt(reader, FindColumn("data_zamowienia"));
        var dataDostawyInt = GetNullableInt(reader, FindColumn("data_dostawy"));
        var dataPlatnosciInt = GetNullableInt(reader, FindColumn("data_platnosci"));
        var dataFakturyInt = GetNullableInt(reader, FindColumn("data_faktury"));

        var dataZam = ClarionDateConverter.ClarionIntToDate(dataZamInt);
        var dataDostawy = ClarionDateConverter.ClarionIntToDate(dataDostawyInt);
        var dataPlatnosci = ClarionDateConverter.ClarionIntToDate(dataPlatnosciInt);
        var dataFaktury = ClarionDateConverter.ClarionIntToDate(dataFakturyInt);

        return new OrderRow
        {
            id = GetInt(reader, FindColumn("id", "id_zamowienia", "ID") ?? "id"),
            data_zamowienia = dataZam,
            data_zamowienia_txt = dataZam.HasValue ? dataZam.Value.ToString("dd/MM/yyyy") : "",
            nr_zamowienia = GetNullableInt(reader, FindColumn("nr_zamowienia")),
            dostawca = GetNullableString(reader, FindColumn("dostawca")),
            waluta = GetNullableString(reader, FindColumn("waluta")),
            data_dostawy = dataDostawy,
            data_dostawy_txt = dataDostawy.HasValue ? dataDostawy.Value.ToString("dd/MM/yyyy") : "",
            status_zamowienia = GetNullableString(reader, FindColumn("status_zamowienia")),
            status_platnosci = GetNullableString(reader, FindColumn("status_platnosci")),
            data_platnosci = dataPlatnosci,
            data_platnosci_txt = dataPlatnosci.HasValue ? dataPlatnosci.Value.ToString("dd/MM/yyyy") : "",
            nr_faktury = GetNullableString(reader, FindColumn("nr_faktury")),
            data_faktury = dataFaktury,
            data_faktury_txt = dataFaktury.HasValue ? dataFaktury.Value.ToString("dd/MM/yyyy") : "",
            wartosc = GetNullableDecimal(reader, FindColumn("wartosc")),
            uwagi = GetNullableString(reader, FindColumn("uwagi", "uwagi_zam")),
            dla_kogo = GetNullableString(reader, FindColumn("dla_kogo")),
            tabela_nbp = GetNullableString(reader, FindColumn("tabela_nbp")),
            kurs_waluty = GetNullableDecimal(reader, FindColumn("kurs_waluty", "kurs"))
        };
    }

    private static int GetInt(MySqlDataReader reader, string columnName)
    {
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
        if (string.IsNullOrEmpty(columnName)) return null;
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

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetNullableString(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return null;
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

    private static DateTime? GetNullableDate(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            var fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(DateTime))
                return reader.GetDateTime(ordinal);

            var raw = reader.GetValue(ordinal);
            var intVal = Convert.ToInt32(raw);
            if (intVal <= 0) return null;
            var baseDate = new DateTime(1800, 12, 28);
            return baseDate.AddDays(intVal);
        }
        catch
        {
            return null;
        }
    }

    private static string ConvertClarionIntDateToString(int? value)
    {
        if (!value.HasValue || value.Value <= 0)
            return "";

        var raw = value.Value.ToString();
        if (raw.Length == 6)
        {
            var dayPart = raw.Substring(0, 2);
            var monthPart = raw.Substring(2, 2);
            var yearPart = raw.Substring(4, 2);
            if (!int.TryParse(dayPart, out var day) ||
                !int.TryParse(monthPart, out var month) ||
                !int.TryParse(yearPart, out var year))
                return "";
            var fullYear = 2000 + year;
            return $"{day:00}/{month:00}/{fullYear:0000}";
        }

        if (raw.Length == 8)
        {
            var yearPart = raw.Substring(0, 4);
            var monthPart = raw.Substring(4, 2);
            var dayPart = raw.Substring(6, 2);
            if (!int.TryParse(dayPart, out var day) ||
                !int.TryParse(monthPart, out var month) ||
                !int.TryParse(yearPart, out var year))
                return "";
            return $"{day:00}/{month:00}/{year:0000}";
        }

        return "";
    }
}
