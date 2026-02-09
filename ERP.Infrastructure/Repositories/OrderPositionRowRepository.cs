using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Repozytorium odczytu pozycji zamówienia z widoku pozycjezamowienia_V.
/// </summary>
public class OrderPositionRowRepository : IOrderPositionRepository
{
    private readonly DatabaseContext _context;

    public OrderPositionRowRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<OrderPositionRow>> GetByOrderIdAsync(int companyId, int orderId, CancellationToken cancellationToken = default)
    {
        var rows = new List<OrderPositionRow>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT * FROM pozycjezamowienia_V " +
            "WHERE company_id = @CompanyId AND id_zamowienia = @OrderId " +
            "ORDER BY id_pozycji_zamowienia",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        command.Parameters.AddWithValue("@OrderId", orderId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapToRow(reader));
        }

        return rows;
    }

    private static OrderPositionRow MapToRow(MySqlDataReader reader)
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

        return new OrderPositionRow
        {
            id_pozycji_zamowienia = GetInt(reader, FindColumn("id_pozycji_zamowienia", "id", "ID") ?? "id_pozycji_zamowienia"),
            company_id = GetNullableInt(reader, FindColumn("company_id", "id_firmy")),
            id_zamowienia = GetNullableInt(reader, FindColumn("id_zamowienia")),
            id_towaru = GetNullableInt(reader, FindColumn("id_towaru")),
            data_dostawy_pozycji = GetNullableInt(reader, FindColumn("data_dostawy_pozycji")),
            data_dostawy_pozycji_txt = ConvertClarionIntDateToString(GetNullableInt(reader, FindColumn("data_dostawy_pozycji"))),
            towar_nazwa_draco = GetNullableString(reader, FindColumn("towar_nazwa_draco")),
            towar = GetNullableString(reader, FindColumn("towar")),
            towar_nazwa_ENG = GetNullableString(reader, FindColumn("towar_nazwa_ENG", "towar_nazwa_eng")),
            jednostki_zamawiane = GetNullableString(reader, FindColumn("jednostki_zamawiane")),
            ilosc_zamawiana = GetNullableDecimal(reader, FindColumn("ilosc_zamawiana")),
            ilosc_dostarczona = GetNullableDecimal(reader, FindColumn("ilosc_dostarczona")),
            cena_zamawiana = GetNullableDecimal(reader, FindColumn("cena_zamawiana")),
            status_towaru = GetNullableString(reader, FindColumn("status_towaru")),
            jednostki_zakupu = GetNullableString(reader, FindColumn("jednostki_zakupu")),
            ilosc_zakupu = GetNullableDecimal(reader, FindColumn("ilosc_zakupu")),
            cena_zakupu = GetNullableDecimal(reader, FindColumn("cena_zakupu")),
            wartsc_zakupu = GetNullableDecimal(reader, FindColumn("wartsc_zakupu")),
            cena_zakupu_pln = GetNullableDecimal(reader, FindColumn("cena_zakupu_pln")),
            przelicznik_m_kg = GetNullableDecimal(reader, FindColumn("przelicznik_m_kg")),
            cena_zakupu_PLN_nowe_jednostki = GetNullableDecimal(reader, FindColumn("cena_zakupu_PLN_nowe_jednostki", "cena_zakupu_pln_nowe_jednostki")),
            uwagi = GetNullableString(reader, FindColumn("uwagi")),
            dostawca_pozycji = GetNullableString(reader, FindColumn("dostawca_pozycji")),
            stawka_vat = GetNullableString(reader, FindColumn("stawka_vat")),
            ciezar_jednostkowy = GetNullableDecimal(reader, FindColumn("ciezar_jednostkowy")),
            ilosc_w_opakowaniu = GetNullableDecimal(reader, FindColumn("ilosc_w_opakowaniu")),
            id_zamowienia_hala = GetNullableInt(reader, FindColumn("id_zamowienia_hala")),
            id_pozycji_pozycji_oferty = GetNullableInt(reader, FindColumn("id_pozycji_pozycji_oferty")),
            zaznacz_do_kopiowania = GetNullableInt(reader, FindColumn("zaznacz_do_kopiowania")),
            skopiowano_do_magazynu = GetNullableBool(reader, FindColumn("skopiowano_do_magazynu")),
            dlugosc = GetNullableDecimal(reader, FindColumn("dlugosc"))
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

    private static bool? GetNullableBool(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
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
