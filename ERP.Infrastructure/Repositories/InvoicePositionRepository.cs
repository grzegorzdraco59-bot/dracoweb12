using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Repozytorium odczytu pozycji faktur (tabela pozycjefaktury).
/// </summary>
public class InvoicePositionRepository : IInvoicePositionRepository
{
    private readonly DatabaseContext _context;

    public InvoicePositionRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<InvoicePositionDto>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var list = new List<InvoicePositionDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT id_pozycji_faktury, id_faktury, Nazwa_towaru, Nazwa_towaru_eng, jednostki, ilosc, cena_netto, rabat, stawka_vat, netto_poz, vat_poz, brutto_poz " +
            "FROM pozycjefaktury WHERE id_faktury = @InvoiceId ORDER BY id_pozycji_faktury",
            connection);
        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapToDto(reader));
        }
        return list;
    }

    private static InvoicePositionDto MapToDto(MySqlDataReader reader)
    {
        return new InvoicePositionDto
        {
            Id = GetInt(reader, "id_pozycji_faktury"),
            InvoiceId = GetInt(reader, "id_faktury"),
            NazwaTowaru = GetNullableString(reader, "Nazwa_towaru") ?? "",
            NazwaTowaruEng = GetNullableString(reader, "Nazwa_towaru_eng"),
            Jednostki = GetNullableString(reader, "jednostki") ?? "szt",
            Ilosc = GetDecimal(reader, "ilosc"),
            CenaNetto = GetDecimal(reader, "cena_netto"),
            Rabat = GetDecimal(reader, "rabat"),
            StawkaVat = GetNullableString(reader, "stawka_vat"),
            NettoPoz = GetDecimal(reader, "netto_poz"),
            VatPoz = GetDecimal(reader, "vat_poz"),
            BruttoPoz = GetDecimal(reader, "brutto_poz")
        };
    }

    private static int GetInt(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    private static decimal GetDecimal(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch { return null; }
    }
}
