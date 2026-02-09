using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Services;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Pozycje Oferty (OfferPosition) używająca MySqlConnector
/// </summary>
public class OfferPositionRepository : IOfferPositionRepository
{
    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;
    private readonly IIdGenerator _idGenerator;

    public OfferPositionRepository(DatabaseContext context, IUserContext userContext, IIdGenerator idGenerator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public async Task<OfferPosition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT p.id, COALESCE(p.oferta_id, p.ID_oferta) AS OfertaId, p.id_firmy, p.id_towaru, p.id_dostawcy, p.kod_towaru, " +
            "p.Nazwa, p.Nazwa_ENG, p.jednostki, p.jednostki_en, COALESCE(p.ilosc, p.Sztuki) AS ilosc, COALESCE(p.cena_netto, p.Cena) AS cena_netto, p.Rabat, p.Cena_po_rabacie, " +
            "COALESCE(p.netto_poz, p.Cena_po_rabacie_i_sztukach) AS netto_poz, p.stawka_vat, COALESCE(p.vat_poz, p.vat) AS vat_poz, COALESCE(p.brutto_poz, p.cena_brutto) AS brutto_poz, " +
            "p.Uwagi_oferta, p.uwagi_faktura, p.inne1, p.nr_zespolu " +
            "FROM apozycjeoferty_V p WHERE p.id = @Id AND p.id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "SELECT p.id, COALESCE(p.oferta_id, p.ID_oferta) AS OfertaId, p.id_firmy, p.id_towaru, p.id_dostawcy, p.kod_towaru, " +
            "p.Nazwa, p.Nazwa_ENG, p.jednostki, p.jednostki_en, COALESCE(p.ilosc, p.Sztuki) AS ilosc, COALESCE(p.cena_netto, p.Cena) AS cena_netto, p.Rabat, p.Cena_po_rabacie, " +
            "COALESCE(p.netto_poz, p.Cena_po_rabacie_i_sztukach) AS netto_poz, p.stawka_vat, COALESCE(p.vat_poz, p.vat) AS vat_poz, COALESCE(p.brutto_poz, p.cena_brutto) AS brutto_poz, " +
            "p.Uwagi_oferta, p.uwagi_faktura, p.inne1, p.nr_zespolu " +
            "FROM apozycjeoferty_V p WHERE COALESCE(p.oferta_id, p.ID_oferta) = @OfferId AND p.id_firmy = @CompanyId " +
            "ORDER BY p.nr_zespolu, p.id",
            connection);
        command.Parameters.AddWithValue("@OfferId", offerId);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "SELECT p.id, COALESCE(p.oferta_id, p.ID_oferta) AS OfertaId, p.id_firmy, p.id_towaru, p.id_dostawcy, p.kod_towaru, " +
            "p.Nazwa, p.Nazwa_ENG, p.jednostki, p.jednostki_en, COALESCE(p.ilosc, p.Sztuki) AS ilosc, COALESCE(p.cena_netto, p.Cena) AS cena_netto, p.Rabat, p.Cena_po_rabacie, " +
            "COALESCE(p.netto_poz, p.Cena_po_rabacie_i_sztukach) AS netto_poz, p.stawka_vat, COALESCE(p.vat_poz, p.vat) AS vat_poz, COALESCE(p.brutto_poz, p.cena_brutto) AS brutto_poz, " +
            "p.Uwagi_oferta, p.uwagi_faktura, p.inne1, p.nr_zespolu " +
            "FROM apozycjeoferty_V p WHERE p.id_firmy = @CompanyId " +
            "ORDER BY COALESCE(p.oferta_id, p.ID_oferta), p.nr_zespolu, p.id",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            positions.Add(MapToOfferPosition(reader));
        }

        return positions;
    }

    public async Task<int> AddAsync(OfferPosition offerPosition, CancellationToken cancellationToken = default)
    {
        ValidatePositionForSave(offerPosition);

        var connection = await _context.CreateConnectionAsync();
        await using var _ = connection;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var newId = (int)await _idGenerator.GetNextIdAsync("apozycjeoferty", connection, transaction, cancellationToken);

            // Mapowanie: oferta_id->ID_oferta, ilosc->Sztuki, cena_netto->Cena, vat_pozycji->vat, brutto_pozycji->cena_brutto, netto_pozycji->Cena_po_rabacie_i_sztukach
            var command = new MySqlCommand(
                "INSERT INTO apozycjeoferty (id_pozycja_oferty, id_firmy, ID_oferta, id_towaru, id_dostawcy, kod_towaru, " +
                "Nazwa, Nazwa_ENG, jednostki, jednostki_en, Sztuki, Cena, Rabat, Cena_po_rabacie, " +
                "Cena_po_rabacie_i_sztukach, stawka_vat, vat, cena_brutto, Uwagi_oferta, uwagi_faktura, " +
                "inne1, nr_zespolu) " +
                "VALUES (@Id, @CompanyId, @OfferId, @ProductId, @SupplierId, @ProductCode, @Name, @NameEng, " +
                "@Unit, @UnitEng, @Sztuki, @Cena, @Discount, @PriceAfterDiscount, " +
                "@Cena_po_rabacie_i_sztukach, @VatRate, @Vat, @Cena_brutto, @OfferNotes, @InvoiceNotes, " +
                "@Other1, @GroupNumber)",
                connection, transaction);
            command.Parameters.AddWithValue("@Id", newId);
            AddOfferPositionParameters(command, offerPosition);

            await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(OfferPosition offerPosition, CancellationToken cancellationToken = default)
    {
        ValidatePositionForSave(offerPosition);

        await using var connection = await _context.CreateConnectionAsync();
        // Mapowanie: oferta_id->ID_oferta, ilosc->Sztuki, cena_netto->Cena, vat_pozycji->vat, brutto_pozycji->cena_brutto, netto_pozycji->Cena_po_rabacie_i_sztukach
        var command = new MySqlCommand(
            "UPDATE apozycjeoferty SET " +
            "ID_oferta = @OfferId, id_towaru = @ProductId, id_dostawcy = @SupplierId, kod_towaru = @ProductCode, " +
            "Nazwa = @Name, Nazwa_ENG = @NameEng, jednostki = @Unit, jednostki_en = @UnitEng, " +
            "Sztuki = @Sztuki, Cena = @Cena, Rabat = @Discount, Cena_po_rabacie = @PriceAfterDiscount, " +
            "Cena_po_rabacie_i_sztukach = @Cena_po_rabacie_i_sztukach, stawka_vat = @VatRate, " +
            "vat = @Vat, cena_brutto = @Cena_brutto, " +
            "Uwagi_oferta = @OfferNotes, uwagi_faktura = @InvoiceNotes, inne1 = @Other1, nr_zespolu = @GroupNumber " +
            "WHERE id_pozycja_oferty = @Id AND id_firmy = @CompanyId",
            connection);

        command.Parameters.AddWithValue("@Id", offerPosition.Id);
        AddOfferPositionParameters(command, offerPosition);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    /// <summary>Walidacja przed zapisem: id_oferty>0, sztuki/ilosc>0, cena>=0, vat>=0.</summary>
    private static void ValidatePositionForSave(OfferPosition offerPosition)
    {
        if (offerPosition.OfferId <= 0)
            throw new ArgumentException("ID oferty musi być większe od 0.", nameof(offerPosition));
        var ilosc = offerPosition.Ilosc ?? 0m;
        if (ilosc <= 0)
            throw new ArgumentException("Ilość (sztuki) musi być większa od 0.", nameof(offerPosition));
        var cena = offerPosition.CenaNetto ?? 0m;
        if (cena < 0)
            throw new ArgumentException("Cena nie może być ujemna.", nameof(offerPosition));
        var vat = offerPosition.VatPoz ?? 0m;
        if (vat < 0)
            throw new ArgumentException("VAT pozycji nie może być ujemny.", nameof(offerPosition));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM apozycjeoferty WHERE id_pozycja_oferty = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task DeleteByOfferIdAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM apozycjeoferty WHERE (oferta_id = @OfferId OR ID_oferta = @OfferId) AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@OfferId", offerId);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM apozycjeoferty WHERE id_pozycja_oferty = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
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

    /// <summary>Odczyt Id (widok apozycjeoferty_V: id = id_pozycja_oferty) jako int dla Entity.</summary>
    private static int GetIdFromReader(MySqlDataReader reader)
    {
        var name = GetColumnName(reader, "id", "Id");
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : (int)reader.GetInt64(ordinal);
    }

    /// <summary>Odczyt OfertaId (alias p.oferta_id, apozycjeoferty.oferta_id) jako int dla Entity.</summary>
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

    /// <summary>Jawny zapis: model.Ilosc->Sztuki, model.CenaNetto->Cena, model.NettoPoz->Cena_po_rabacie_i_sztukach, model.VatPoz->vat, model.BruttoPoz->cena_brutto.</summary>
    private static void AddOfferPositionParameters(MySqlCommand command, OfferPosition offerPosition)
    {
        command.Parameters.AddWithValue("@CompanyId", offerPosition.CompanyId);
        command.Parameters.AddWithValue("@OfferId", offerPosition.OfferId);
        command.Parameters.AddWithValue("@ProductId", offerPosition.ProductId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierId", offerPosition.SupplierId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductCode", offerPosition.ProductCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Name", offerPosition.Name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@NameEng", offerPosition.NameEng ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Unit", offerPosition.Unit);
        command.Parameters.AddWithValue("@UnitEng", offerPosition.UnitEng ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Sztuki", offerPosition.Ilosc ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Cena", offerPosition.CenaNetto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Discount", offerPosition.Discount ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PriceAfterDiscount", offerPosition.PriceAfterDiscount ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Cena_po_rabacie_i_sztukach", offerPosition.NettoPoz ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatRate", offerPosition.VatRate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Vat", offerPosition.VatPoz ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Cena_brutto", offerPosition.BruttoPoz ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferNotes", offerPosition.OfferNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@InvoiceNotes", offerPosition.InvoiceNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Other1", offerPosition.Other1 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@GroupNumber", offerPosition.GroupNumber ?? (object)DBNull.Value);
    }

    /// <summary>Rabat w %. Używane przez OfferTotalsService do przeliczania linii.</summary>
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
