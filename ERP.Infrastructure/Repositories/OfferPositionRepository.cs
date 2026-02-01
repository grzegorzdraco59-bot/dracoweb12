using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Pozycje Oferty (OfferPosition) używająca MySqlConnector
/// </summary>
public class OfferPositionRepository : IOfferPositionRepository
{
    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;

    public OfferPositionRepository(DatabaseContext context, IUserContext userContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<OfferPosition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT p.id AS Id, p.oferta_id AS OfertaId, p.id_firmy, p.id_towaru, p.id_dostawcy, p.kod_towaru, " +
            "p.Nazwa, p.Nazwa_ENG, p.jednostki, p.jednostki_en, p.ilosc, p.cena_netto, p.Rabat, p.Cena_po_rabacie, " +
            "p.netto_poz, p.stawka_vat, p.vat_poz, p.brutto_poz, " +
            "p.Uwagi_oferta, p.uwagi_faktura, p.inne1, p.nr_zespolu " +
            "FROM ofertypozycje p WHERE p.id = @Id AND p.id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToOfferPosition(reader);
        }

        return null;
    }

    public async Task<IEnumerable<OfferPosition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await GetByCompanyIdAsync(GetCurrentCompanyId(), cancellationToken);
    }

    public async Task<IEnumerable<OfferPosition>> GetByOfferIdAsync(int offerId, CancellationToken cancellationToken = default)
    {
        var positions = new List<OfferPosition>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT p.id AS Id, p.oferta_id AS OfertaId, p.id_firmy, p.id_towaru, p.id_dostawcy, p.kod_towaru, " +
            "p.Nazwa, p.Nazwa_ENG, p.jednostki, p.jednostki_en, p.ilosc, p.cena_netto, p.Rabat, p.Cena_po_rabacie, " +
            "p.netto_poz, p.stawka_vat, p.vat_poz, p.brutto_poz, " +
            "p.Uwagi_oferta, p.uwagi_faktura, p.inne1, p.nr_zespolu " +
            "FROM ofertypozycje p WHERE p.oferta_id = @OfferId AND p.id_firmy = @CompanyId " +
            "ORDER BY p.nr_zespolu, p.id",
            connection);
        command.Parameters.AddWithValue("@OfferId", offerId);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            positions.Add(MapToOfferPosition(reader));
        }

        return positions;
    }

    public async Task<IEnumerable<OfferPosition>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var positions = new List<OfferPosition>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT p.id AS Id, p.oferta_id AS OfertaId, p.id_firmy, p.id_towaru, p.id_dostawcy, p.kod_towaru, " +
            "p.Nazwa, p.Nazwa_ENG, p.jednostki, p.jednostki_en, p.ilosc, p.cena_netto, p.Rabat, p.Cena_po_rabacie, " +
            "p.netto_poz, p.stawka_vat, p.vat_poz, p.brutto_poz, " +
            "p.Uwagi_oferta, p.uwagi_faktura, p.inne1, p.nr_zespolu " +
            "FROM ofertypozycje p WHERE p.id_firmy = @CompanyId " +
            "ORDER BY p.oferta_id, p.nr_zespolu, p.id",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            positions.Add(MapToOfferPosition(reader));
        }

        return positions;
    }

    public async Task<int> AddAsync(OfferPosition offerPosition, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO ofertypozycje (id_firmy, oferta_id, id_towaru, id_dostawcy, kod_towaru, " +
            "Nazwa, Nazwa_ENG, jednostki, jednostki_en, ilosc, cena_netto, Rabat, Cena_po_rabacie, " +
            "netto_poz, stawka_vat, vat_poz, brutto_poz, Uwagi_oferta, uwagi_faktura, " +
            "inne1, nr_zespolu) " +
            "VALUES (@CompanyId, @OfferId, @ProductId, @SupplierId, @ProductCode, @Name, @NameEng, " +
            "@Unit, @UnitEng, @Ilosc, @CenaNetto, @Discount, @PriceAfterDiscount, " +
            "@NettoPoz, @VatRate, @VatPoz, @BruttoPoz, @OfferNotes, @InvoiceNotes, " +
            "@Other1, @GroupNumber); " +
            "SELECT LAST_INSERT_ID();",
            connection);
        
        AddOfferPositionParameters(command, offerPosition);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(OfferPosition offerPosition, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE ofertypozycje SET " +
            "oferta_id = @OfferId, id_towaru = @ProductId, id_dostawcy = @SupplierId, kod_towaru = @ProductCode, " +
            "Nazwa = @Name, Nazwa_ENG = @NameEng, jednostki = @Unit, jednostki_en = @UnitEng, " +
            "ilosc = @Ilosc, cena_netto = @CenaNetto, Rabat = @Discount, Cena_po_rabacie = @PriceAfterDiscount, " +
            "netto_poz = @NettoPoz, stawka_vat = @VatRate, " +
            "vat_poz = @VatPoz, brutto_poz = @BruttoPoz, " +
            "Uwagi_oferta = @OfferNotes, uwagi_faktura = @InvoiceNotes, inne1 = @Other1, nr_zespolu = @GroupNumber " +
            "WHERE id = @Id AND id_firmy = @CompanyId",
            connection);
        
        command.Parameters.AddWithValue("@Id", offerPosition.Id);
        AddOfferPositionParameters(command, offerPosition);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM ofertypozycje WHERE id = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByOfferIdAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM ofertypozycje WHERE oferta_id = @OfferId AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@OfferId", offerId);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM ofertypozycje WHERE id = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private int GetCurrentCompanyId()
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
        return companyId.Value;
    }

    private static OfferPosition MapToOfferPosition(MySqlDataReader reader)
    {
        int id = GetIdFromReader(reader);
        int companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        int offerId = GetOfferIdFromReader(reader);
        string unit = reader.GetString(reader.GetOrdinal("jednostki"));
        
        var position = new OfferPosition(companyId, offerId, unit);
        
        // Użyj refleksji do ustawienia Id
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProperty?.SetValue(position, id);
        
        position.UpdateProductInfo(
            GetNullableInt(reader, "id_towaru"),
            GetNullableInt(reader, "id_dostawcy"),
            GetNullableString(reader, "kod_towaru"),
            GetNullableString(reader, "Nazwa"),
            GetNullableString(reader, "Nazwa_ENG")
        );
        
        position.UpdateUnits(
            reader.GetString(reader.GetOrdinal("jednostki")),
            GetNullableString(reader, "jednostki_en")
        );
        
        position.UpdatePricing(
            GetNullableDecimal(reader, "ilosc"),
            GetNullableDecimal(reader, "cena_netto"),
            GetNullableDecimal(reader, "Rabat"),
            GetNullableDecimal(reader, "Cena_po_rabacie"),
            GetNullableDecimal(reader, "netto_poz")
        );
        
        position.UpdateVatInfo(
            GetNullableString(reader, "stawka_vat"),
            GetNullableDecimal(reader, "vat_poz"),
            GetNullableDecimal(reader, "brutto_poz")
        );
        
        position.UpdateNotes(
            GetNullableString(reader, "Uwagi_oferta"),
            GetNullableString(reader, "uwagi_faktura"),
            GetNullableString(reader, "inne1")
        );
        
        position.UpdateGroupNumber(GetNullableDecimal(reader, "nr_zespolu"));
        
        return position;
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

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    /// <summary>Odczyt Id (alias p.id, ofertypozycje.id, BIGINT) jako int dla Entity.</summary>
    private static int GetIdFromReader(MySqlDataReader reader)
    {
        var name = GetColumnName(reader, "Id", "id");
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : (int)reader.GetInt64(ordinal);
    }

    /// <summary>Odczyt OfertaId (alias p.oferta_id, ofertypozycje.oferta_id) jako int dla Entity.</summary>
    private static int GetOfferIdFromReader(MySqlDataReader reader)
    {
        var name = GetColumnName(reader, "OfertaId", "oferta_id");
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : (int)reader.GetInt64(ordinal);
    }

    private static string GetColumnName(MySqlDataReader reader, string preferred, string fallback)
    {
        for (var i = 0; i < reader.FieldCount; i++)
            if (string.Equals(reader.GetName(i), preferred, StringComparison.OrdinalIgnoreCase))
                return reader.GetName(i);
        return fallback;
    }

    private static void AddOfferPositionParameters(MySqlCommand command, OfferPosition offerPosition)
    {
        var ilosc = offerPosition.Ilosc ?? 0m;
        var cenaNetto = offerPosition.CenaNetto ?? 0m;
        var rabat = offerPosition.Discount ?? 0m;
        var (nettoPoz, vatPoz, bruttoPoz) = ComputePositionAmounts(ilosc, cenaNetto, rabat, offerPosition.VatRate);

        command.Parameters.AddWithValue("@CompanyId", offerPosition.CompanyId);
        command.Parameters.AddWithValue("@OfferId", offerPosition.OfferId);
        command.Parameters.AddWithValue("@ProductId", offerPosition.ProductId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierId", offerPosition.SupplierId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductCode", offerPosition.ProductCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Name", offerPosition.Name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@NameEng", offerPosition.NameEng ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Unit", offerPosition.Unit);
        command.Parameters.AddWithValue("@UnitEng", offerPosition.UnitEng ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Ilosc", offerPosition.Ilosc ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CenaNetto", offerPosition.CenaNetto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Discount", offerPosition.Discount ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PriceAfterDiscount", offerPosition.PriceAfterDiscount ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@NettoPoz", nettoPoz);
        command.Parameters.AddWithValue("@VatRate", offerPosition.VatRate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatPoz", vatPoz);
        command.Parameters.AddWithValue("@BruttoPoz", bruttoPoz);
        command.Parameters.AddWithValue("@OfferNotes", offerPosition.OfferNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@InvoiceNotes", offerPosition.InvoiceNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Other1", offerPosition.Other1 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@GroupNumber", offerPosition.GroupNumber ?? (object)DBNull.Value);
    }

    /// <summary>Rabat w %. netto_poz = ROUND(ilosc * cena_netto * (1 - rabat/100), 2), vat_poz = ROUND(netto_poz * stawka_vat/100, 2), brutto_poz = netto_poz + vat_poz.</summary>
    private static (decimal nettoPoz, decimal vatPoz, decimal bruttoPoz) ComputePositionAmounts(decimal ilosc, decimal cenaNetto, decimal rabatPercent, string? stawkaVat)
    {
        var netto0 = ilosc * cenaNetto;
        var nettoPoRabacie = netto0 * (1m - rabatPercent / 100m);
        var nettoPoz = Math.Round(nettoPoRabacie, 2);
        var vatRate = ParseVatRate(stawkaVat);
        var vatPoz = Math.Round(nettoPoz * vatRate / 100m, 2);
        var bruttoPoz = nettoPoz + vatPoz;
        return (nettoPoz, vatPoz, bruttoPoz);
    }

    private static decimal ParseVatRate(string? stawkaVat)
    {
        if (string.IsNullOrWhiteSpace(stawkaVat)) return 0m;
        var s = stawkaVat.Trim().TrimEnd('%');
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rate) ? rate : 0m;
    }
}
