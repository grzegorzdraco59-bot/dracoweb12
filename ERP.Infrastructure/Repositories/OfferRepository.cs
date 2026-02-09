using System.IO;
using System.Text;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Services;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Oferty (Offer) używająca MySqlConnector.
/// Repozytoria zawierają tylko operacje CRUD - logika biznesowa jest w warstwie Application.
/// Rozdzielenie: SELECT z aoferty_V (widok), INSERT/UPDATE/DELETE do aoferty (tabela bazowa).
/// ID dla INSERT pobierane z id_sequences.
/// </summary>
public class OfferRepository : IOfferRepository
{
    private static bool _aofertyVVerified;
    private static bool _diagnosticRun;
    private readonly DatabaseContext _context;
    private readonly IIdGenerator _idGenerator;

    public OfferRepository(DatabaseContext context, IIdGenerator idGenerator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public async Task<Offer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await VerifyAofertyVAsync(cancellationToken);
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, company_id, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, " +
            "odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, " +
            "odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat, " +
            "total_vat, total_brutto, Cena_calkowita AS sum_netto, total_vat AS sum_vat, total_brutto AS sum_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi, " +
            "do_faktury, historia, status " +
            "FROM aoferty_V WHERE id = @Id AND company_id = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToOffer(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Offer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Użyj GetByCompanyIdAsync zamiast GetAllAsync. Repozytoria wymagają companyId jako parametru.");
    }

    public async Task<IEnumerable<Offer>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await VerifyAofertyVAsync(cancellationToken);
        var offers = new List<Offer>();
        await using var connection = await _context.CreateConnectionAsync();
        await RunAofertyVDiagnosticAsync(connection, cancellationToken);
        var command = new MySqlCommand(
            "SELECT id, company_id, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, " +
            "odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, " +
            "odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat, " +
            "total_vat, total_brutto, Cena_calkowita AS sum_netto, total_vat AS sum_vat, total_brutto AS sum_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi, " +
            "do_faktury, historia, status " +
            "FROM aoferty_V WHERE company_id = @CompanyId ORDER BY id DESC, Nr_oferty DESC",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            offers.Add(MapToOffer(reader));
        }

        return offers;
    }

    public async Task<int> AddAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        var connection = await _context.CreateConnectionAsync();
        await using var _ = connection;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Kolumny z DEFAULT w DB (do_faktury, historia, status) – pomijamy w INSERT, baza użyje wartości domyślnych.
        var sql = "INSERT INTO aoferty (id_oferta, id_firmy, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, " +
            "odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, " +
            "odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat, " +
            "total_vat, total_brutto, sum_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi) " +
            "VALUES (@Id, @CompanyId, @ForProforma, @ForOrder, @OfferDate, @OfferNumber, @CustomerId, " +
            "@CustomerName, @CustomerStreet, @CustomerPostalCode, @CustomerCity, @CustomerCountry, " +
            "@CustomerNip, @CustomerEmail, @Currency, @TotalPrice, @VatRate, @TotalVat, @TotalBrutto, " +
            "@SumBrutto, @OfferNotes, @AdditionalData, @Operator, @TradeNotes)";
        MySqlCommand? command = null;
        try
        {
            var newId = (int)await _idGenerator.GetNextIdAsync("aoferty", connection, transaction, cancellationToken);

            command = new MySqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@Id", newId);
            AddOfferParameters(command, offer);

            await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return newId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            var paramSb = new StringBuilder();
            if (command != null)
            {
                foreach (MySqlParameter p in command.Parameters)
                    paramSb.AppendLine($"  {p.ParameterName} = {p.Value}");
            }
            else
            {
                paramSb.AppendLine("  (command = null – błąd przed utworzeniem)");
            }
            var msg = $"ex.ToString():\r\n{ex}\r\n\r\n--- SQL ---\r\n{sql}\r\n\r\n--- PARAMETRY ---\r\n{paramSb}";
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "insert_oferta_error.txt");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.WriteAllText(logPath, msg);
            }
            catch { /* ignoruj */ }
#if WINDOWS
            System.Windows.MessageBox.Show(msg, "INSERT oferta - błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
#endif
            throw;
        }
    }

    public async Task UpdateAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE aoferty SET " +
            "do_proformy = @ForProforma, do_zlecenia = @ForOrder, Data_oferty = @OfferDate, " +
            "Nr_oferty = @OfferNumber, odbiorca_ID_odbiorcy = @CustomerId, odbiorca_nazwa = @CustomerName, " +
            "odbiorca_ulica = @CustomerStreet, odbiorca_kod_poczt = @CustomerPostalCode, " +
            "odbiorca_miasto = @CustomerCity, odbiorca_panstwo = @CustomerCountry, odbiorca_nip = @CustomerNip, " +
            "odbiorca_mail = @CustomerEmail, Waluta = @Currency, Cena_calkowita = @TotalPrice, " +
            "stawka_vat = @VatRate, total_vat = @TotalVat, total_brutto = @TotalBrutto, sum_brutto = @SumBrutto, " +
            "uwagi_do_oferty = @OfferNotes, dane_dodatkowe = @AdditionalData, operator = @Operator, " +
            "uwagi_targi = @TradeNotes, do_faktury = @ForInvoice, historia = @History, status = @Status " +
            "WHERE id_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        
        command.Parameters.AddWithValue("@Id", offer.Id);
        AddOfferParameters(command, offer);
        
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM aoferty WHERE id_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task SetStatusAsync(int id, int companyId, OfferStatus status, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE aoferty SET status = @Status WHERE id_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        command.Parameters.AddWithValue("@Status", OfferStatusMapping.ToDb(status));
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task SetFlagsAsync(int offerId, int companyId, bool? forProforma, bool? forOrder, bool forInvoice, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE aoferty SET do_proformy = @ForProforma, do_zlecenia = @ForOrder, do_faktury = @ForInvoice WHERE id_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", offerId);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        command.Parameters.AddWithValue("@ForProforma", forProforma ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ForOrder", forOrder ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ForInvoice", forInvoice);
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM aoferty_V WHERE id = @Id AND company_id = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task<int?> GetNextOfferNumberForDateAsync(int offerDate, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COALESCE(MAX(Nr_oferty), 0) + 1 " +
            "FROM aoferty_V " +
            "WHERE company_id = @CompanyId AND Data_oferty = @OfferDate",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        command.Parameters.AddWithValue("@OfferDate", offerDate);
        
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return 1;
        
        return Convert.ToInt32(result);
    }

    public async Task<IEnumerable<Offer>> SearchByCompanyIdAsync(int companyId, string? searchText, int limit = 200, CancellationToken cancellationToken = default)
    {
        await VerifyAofertyVAsync(cancellationToken);
        var offers = new List<Offer>();
        await using var connection = await _context.CreateConnectionAsync();

        var sql = @"
SELECT id, company_id, do_proformy, do_zlecenia, Data_oferty, Nr_oferty,
       odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto,
       odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat,
       total_vat, total_brutto, Cena_calkowita AS sum_netto, total_vat AS sum_vat, total_brutto AS sum_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi,
       do_faktury, historia, status
FROM aoferty_V
WHERE company_id = @CompanyId
  AND (@Q IS NULL OR @Q = '' OR (
    odbiorca_nazwa LIKE CONCAT('%', @Q, '%')
    OR odbiorca_ulica LIKE CONCAT('%', @Q, '%')
    OR odbiorca_panstwo LIKE CONCAT('%', @Q, '%')
    OR odbiorca_miasto LIKE CONCAT('%', @Q, '%')
  ))
ORDER BY id DESC
LIMIT @Limit";

        var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        command.Parameters.AddWithValue("@Q", (object?)searchText ?? DBNull.Value);
        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            offers.Add(MapToOffer(reader));
        }

        return offers;
    }

    private static Offer MapToOffer(MySqlDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("id"));
        int companyId = reader.GetInt32(reader.GetOrdinal("company_id"));
        string @operator = reader.GetString(reader.GetOrdinal("operator"));
        
        var offer = new Offer(companyId, @operator);
        
        // Użyj refleksji do ustawienia Id i innych właściwości
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProperty?.SetValue(offer, id);
        
        offer.UpdateOfferInfo(
            GetNullableInt(reader, "Data_oferty"),
            GetNullableInt(reader, "Nr_oferty"),
            GetNullableString(reader, "Waluta")
        );
        
        offer.UpdateCustomerInfo(
            GetNullableInt(reader, "odbiorca_ID_odbiorcy"),
            GetNullableString(reader, "odbiorca_nazwa"),
            GetNullableString(reader, "odbiorca_ulica"),
            GetNullableString(reader, "odbiorca_kod_poczt"),
            GetNullableString(reader, "odbiorca_miasto"),
            GetNullableString(reader, "odbiorca_panstwo"),
            GetNullableString(reader, "odbiorca_nip"),
            GetNullableString(reader, "odbiorca_mail")
        );
        
        var sumNetto = GetNullableDecimal(reader, "sum_netto");
        var sumVat = GetNullableDecimal(reader, "sum_vat");
        offer.UpdatePricing(
            sumNetto ?? GetNullableDecimal(reader, "Cena_calkowita"),
            GetNullableDecimal(reader, "stawka_vat"),
            sumVat ?? GetNullableDecimal(reader, "total_vat"),
            GetNullableDecimal(reader, "total_brutto")
        );
        offer.UpdateSumBrutto(GetNullableDecimal(reader, "sum_brutto"));
        
        offer.UpdateFlags(
            GetNullableBool(reader, "do_proformy"),
            GetNullableBool(reader, "do_zlecenia"),
            reader.GetBoolean(reader.GetOrdinal("do_faktury"))
        );
        
        offer.UpdateNotes(
            GetNullableString(reader, "uwagi_do_oferty"),
            GetNullableString(reader, "dane_dodatkowe"),
            reader.GetString(reader.GetOrdinal("uwagi_targi"))
        );
        
        offer.UpdateHistory(reader.GetString(reader.GetOrdinal("historia")));

        var statusStr = GetNullableString(reader, "status");
        offer.UpdateStatus(OfferStatusMapping.FromDb(statusStr));

        return offer;
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static bool? GetNullableBool(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    /// <summary>Diagnostyka runtime: DATABASE(), SHOW TABLES aoferty%, SELECT id FROM aoferty_V – ten sam connection co ekran ofert.</summary>
    private static async Task RunAofertyVDiagnosticAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        if (_diagnosticRun) return;
        _diagnosticRun = true;
        var sb = new StringBuilder();
        try
        {
            // 1) SELECT DATABASE();
            using (var cmd = new MySqlCommand("SELECT DATABASE()", connection))
            {
                var db = await cmd.ExecuteScalarAsync(cancellationToken);
                sb.AppendLine($"1) DATABASE() = {db ?? "(NULL)"}");
            }
            // 2) SHOW FULL TABLES LIKE 'aoferty%';
            sb.AppendLine("2) SHOW FULL TABLES LIKE 'aoferty%':");
            using (var cmd = new MySqlCommand("SHOW FULL TABLES LIKE 'aoferty%'", connection))
            await using (var r = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await r.ReadAsync(cancellationToken))
                {
                    var name = r.GetString(0);
                    var type = r.GetString(1);
                    sb.AppendLine($"   - {name} ({type})");
                }
            }
            // 3) SELECT id, company_id FROM aoferty_V LIMIT 1;
            using (var cmd3 = new MySqlCommand("SELECT id, company_id FROM aoferty_V LIMIT 1", connection))
            {
                var result = await cmd3.ExecuteScalarAsync(cancellationToken);
                sb.AppendLine($"3) SELECT id, company_id FROM aoferty_V LIMIT 1: OK (wynik={result ?? "(NULL/brak wierszy)"})");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"3) SELECT id, company_id FROM aoferty_V LIMIT 1: BŁĄD - {ex.Message}");
        }
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "aoferty_v_diagnostic.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\r\n{sb}\r\n");
        }
        catch { /* ignoruj */ }
        // MessageBox diagnostyki SELECT wyłączony – błąd INSERT pokazuje osobny MessageBox w AddAsync
    }

    /// <summary>Weryfikacja widoku aoferty_V (id, company_id) – jeden raz przy pierwszym ładowaniu ofert.</summary>
    private async Task VerifyAofertyVAsync(CancellationToken cancellationToken)
    {
        if (_aofertyVVerified) return;
        await using var connection = await _context.CreateConnectionAsync();
        using var cmd = new MySqlCommand("SELECT id, company_id FROM aoferty_V LIMIT 1", connection);
        await cmd.ExecuteScalarAsync(cancellationToken);
        _aofertyVVerified = true;
    }

    /// <summary>Parametry INSERT – wartości domyślne dla kolumn NOT NULL bez DEFAULT w DB, aby uniknąć "cannot be null".</summary>
    private static void AddOfferParameters(MySqlCommand command, Offer offer)
    {
        command.Parameters.AddWithValue("@CompanyId", offer.CompanyId);
        command.Parameters.AddWithValue("@ForProforma", offer.ForProforma ?? false);
        command.Parameters.AddWithValue("@ForOrder", offer.ForOrder ?? false);
        command.Parameters.AddWithValue("@OfferDate", offer.OfferDate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferNumber", offer.OfferNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerId", offer.CustomerId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerName", offer.CustomerName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerStreet", offer.CustomerStreet ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerPostalCode", offer.CustomerPostalCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerCity", offer.CustomerCity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerCountry", offer.CustomerCountry ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerNip", offer.CustomerNip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerEmail", offer.CustomerEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Currency", offer.Currency ?? "PLN");
        command.Parameters.AddWithValue("@TotalPrice", offer.TotalPrice ?? 0m);
        command.Parameters.AddWithValue("@VatRate", offer.VatRate ?? 23m);
        command.Parameters.AddWithValue("@TotalVat", offer.TotalVat ?? 0m);
        command.Parameters.AddWithValue("@TotalBrutto", offer.TotalBrutto ?? 0m);
        command.Parameters.AddWithValue("@SumBrutto", offer.SumBrutto ?? 0m);
        command.Parameters.AddWithValue("@OfferNotes", offer.OfferNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@AdditionalData", offer.AdditionalData ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Operator", offer.Operator ?? "");
        command.Parameters.AddWithValue("@TradeNotes", offer.TradeNotes ?? "");
        // UPDATE używa @ForInvoice, @History, @Status – INSERT pomija (DB DEFAULT), ale parametry muszą być zdefiniowane gdy AddOfferParameters wywołane dla UPDATE
        command.Parameters.AddWithValue("@ForInvoice", offer.ForInvoice);
        command.Parameters.AddWithValue("@History", offer.History ?? "");
        command.Parameters.AddWithValue("@Status", OfferStatusMapping.ToDb(offer.Status));
    }
}
