using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Repozytorium odczytu nagłówków faktur (tabela faktury).
/// </summary>
public class InvoiceRepository : IInvoiceRepository
{
    private readonly DatabaseContext _context;

    public InvoiceRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<InvoiceDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var list = new List<InvoiceDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT Id_faktury, id_firmy, id_oferty, data_faktury, nr_faktury, nr_faktury_text, doc_type, " +
            "odbiorca_nazwa, waluta, kwota_netto, total_vat, kwota_brutto, sum_netto, sum_vat, sum_brutto, operator " +
            "FROM faktury WHERE id_firmy = @CompanyId ORDER BY Id_faktury DESC",
            connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapToDto(reader));
        }
        return list;
    }

    public async Task<IEnumerable<OfferDocumentDto>> GetDocumentsByOfferIdAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        var list = new List<OfferDocumentDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT Id_faktury, doc_type, doc_full_no, data_faktury, sum_brutto, do_zaplaty_brutto " +
            "FROM faktury WHERE id_oferty = @OfferId AND id_firmy = @CompanyId ORDER BY doc_type, Id_faktury",
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new OfferDocumentDto
            {
                InvoiceId = GetInt(reader, "Id_faktury"),
                DocType = GetNullableString(reader, "doc_type") ?? "",
                DocFullNo = GetNullableString(reader, "doc_full_no"),
                DataFaktury = GetNullableInt(reader, "data_faktury"),
                SumBrutto = GetNullableDecimal(reader, "sum_brutto"),
                DoZaplatyBrutto = GetNullableDecimal(reader, "do_zaplaty_brutto")
            });
        }
        return list;
    }

    private static InvoiceDto MapToDto(MySqlDataReader reader)
    {
        return new InvoiceDto
        {
            Id = GetInt(reader, "Id_faktury"),
            CompanyId = GetInt(reader, "id_firmy"),
            IdOferty = GetNullableInt(reader, "id_oferty"),
            DataFaktury = GetNullableInt(reader, "data_faktury"),
            NrFaktury = GetNullableInt(reader, "nr_faktury"),
            NrFakturyText = GetNullableString(reader, "nr_faktury_text"),
            SkrotNazwaFaktury = GetNullableString(reader, "doc_type"),
            OdbiorcaNazwa = GetNullableString(reader, "odbiorca_nazwa"),
            Waluta = GetNullableString(reader, "waluta"),
            KwotaNetto = GetNullableDecimal(reader, "kwota_netto"),
            TotalVat = GetNullableDecimal(reader, "total_vat"),
            KwotaBrutto = GetNullableDecimal(reader, "kwota_brutto"),
            SumNetto = GetNullableDecimal(reader, "sum_netto"),
            SumVat = GetNullableDecimal(reader, "sum_vat"),
            SumBrutto = GetNullableDecimal(reader, "sum_brutto"),
            Operator = GetNullableString(reader, "operator")
        };
    }

    private static int GetInt(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    private static int? GetNullableInt(MySqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
        catch { return null; }
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

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }
        catch { return null; }
    }
}
