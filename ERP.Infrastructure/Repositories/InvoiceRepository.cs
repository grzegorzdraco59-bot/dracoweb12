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
            "SELECT id_faktury, id_firmy, id_oferty, data_faktury, nr_faktury, nr_faktury_text, " +
            "COALESCE(doc_type, skrot_nazwa_faktury) AS doc_type, skrot_nazwa_faktury, " +
            "id_odbiorca, odbiorca_nazwa, odbiorca_mail, waluta, kwota_netto, total_vat, kwota_brutto, sum_netto, sum_vat, sum_brutto, operator " +
            "FROM faktury WHERE id_firmy = @CompanyId ORDER BY data_faktury DESC, nr_faktury DESC",
            connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await using var reader = await cmd.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapToDto(reader));
        }
        return list;
    }

    public async Task<int> GetNextInvoiceNumberAsync(int companyId, string skrotNazwaFaktury, int dataFaktury, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(skrotNazwaFaktury))
            throw new ArgumentException("skrot_nazwa_faktury nie może być puste.", nameof(skrotNazwaFaktury));

        await using var connection = await _context.CreateConnectionAsync();
        // data_faktury: Clarion (dni od 28.12.1800) – YEAR/MONTH z FROM_DAYS(TO_DAYS('1800-12-28') + data_faktury)
        var cmd = new MySqlCommand(
            "SELECT COALESCE(MAX(nr_faktury), 0) + 1 " +
            "FROM faktury " +
            "WHERE id_firmy = @CompanyId AND skrot_nazwa_faktury = @skrot " +
            "AND YEAR(FROM_DAYS(TO_DAYS('1800-12-28') + data_faktury)) = YEAR(FROM_DAYS(TO_DAYS('1800-12-28') + @data_faktury)) " +
            "AND MONTH(FROM_DAYS(TO_DAYS('1800-12-28') + data_faktury)) = MONTH(FROM_DAYS(TO_DAYS('1800-12-28') + @data_faktury))",
            connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        cmd.Parameters.AddWithValue("@skrot", skrotNazwaFaktury.Trim());
        cmd.Parameters.AddWithValue("@data_faktury", dataFaktury);

        var result = await cmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        return result == null || result == DBNull.Value ? 1 : Convert.ToInt32(result);
    }

    public async Task<InvoiceDto?> GetByIdAsync(long invoiceId, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT id_faktury, id_firmy, id_oferty, data_faktury, nr_faktury, nr_faktury_text, " +
            "COALESCE(doc_type, skrot_nazwa_faktury) AS doc_type, skrot_nazwa_faktury, " +
            "id_odbiorca, odbiorca_nazwa, odbiorca_mail, waluta, kwota_netto, total_vat, kwota_brutto, sum_netto, sum_vat, sum_brutto, operator " +
            "FROM faktury WHERE id_faktury = @InvoiceId AND id_firmy = @CompanyId LIMIT 1",
            connection);
        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await using var reader = await cmd.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return MapToDto(reader);
    }

    public async Task<IEnumerable<OfferDocumentDto>> GetDocumentsByOfferIdAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        var list = new List<OfferDocumentDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT id_faktury, doc_type, doc_full_no, data_faktury, sum_brutto, do_zaplaty_brutto " +
            "FROM faktury WHERE id_oferty = @OfferId AND id_firmy = @CompanyId ORDER BY doc_type, id_faktury",
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await using var reader = await cmd.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new OfferDocumentDto
            {
                InvoiceId = (int)GetLong(reader, "id_faktury"),
                DocType = GetNullableString(reader, "doc_type") ?? "",
                DocFullNo = GetNullableString(reader, "doc_full_no"),
                DataFaktury = GetNullableInt(reader, "data_faktury"),
                SumBrutto = GetNullableDecimal(reader, "sum_brutto"),
                DoZaplatyBrutto = GetNullableDecimal(reader, "do_zaplaty_brutto")
            });
        }
        return list;
    }

    public async Task UpdateRecipientAsync(long invoiceId, int companyId, int? odbiorcaId, string? odbiorcaNazwa, string? odbiorcaMail, string? waluta, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "UPDATE faktury SET id_odbiorca = @OdbiorcaId, odbiorca_nazwa = @OdbiorcaNazwa, odbiorca_mail = @OdbiorcaMail, waluta = @Waluta " +
            "WHERE id_faktury = @InvoiceId AND id_firmy = @CompanyId",
            connection);
        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        cmd.Parameters.AddWithValue("@OdbiorcaId", odbiorcaId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OdbiorcaNazwa", odbiorcaNazwa ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OdbiorcaMail", odbiorcaMail ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Waluta", waluta ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    private static InvoiceDto MapToDto(MySqlDataReader reader)
    {
        return new InvoiceDto
        {
            Id = GetLong(reader, "id_faktury"),
            CompanyId = GetInt(reader, "id_firmy"),
            IdOferty = GetNullableInt(reader, "id_oferty"),
            DataFaktury = GetNullableInt(reader, "data_faktury"),
            NrFaktury = GetNullableInt(reader, "nr_faktury"),
            NrFakturyText = GetNullableString(reader, "nr_faktury_text"),
            SkrotNazwaFaktury = GetNullableString(reader, "skrot_nazwa_faktury") ?? GetNullableString(reader, "doc_type"),
            OdbiorcaId = GetNullableInt(reader, "id_odbiorca"),
            OdbiorcaNazwa = GetNullableString(reader, "odbiorca_nazwa"),
            OdbiorcaEmail = GetNullableString(reader, "odbiorca_mail"),
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

    private static long GetLong(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? 0L : reader.GetInt64(ordinal);
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
