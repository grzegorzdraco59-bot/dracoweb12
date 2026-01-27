using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using ERP.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Pozycje Oferty (OfferPosition) używająca MySqlConnector
/// </summary>
public class OfferPositionRepository : IOfferPositionRepository
{
    private readonly DatabaseContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OfferPositionRepository(DatabaseContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<OfferPosition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_pozycja_oferty, id_firmy, ID_oferta, id_towaru, id_dostawcy, kod_towaru, " +
            "Nazwa, Nazwa_ENG, jednostki, jednostki_en, Sztuki, Cena, Rabat, Cena_po_rabacie, " +
            "Cena_po_rabacie_i_sztukach, stawka_vat, vat, cena_brutto, Uwagi_oferta, uwagi_faktura, " +
            "inne1, nr_zespolu " +
            "FROM apozycjeoferty WHERE ID_pozycja_oferty = @Id AND id_firmy = @CompanyId",
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
            "SELECT ID_pozycja_oferty, id_firmy, ID_oferta, id_towaru, id_dostawcy, kod_towaru, " +
            "Nazwa, Nazwa_ENG, jednostki, jednostki_en, Sztuki, Cena, Rabat, Cena_po_rabacie, " +
            "Cena_po_rabacie_i_sztukach, stawka_vat, vat, cena_brutto, Uwagi_oferta, uwagi_faktura, " +
            "inne1, nr_zespolu " +
            "FROM apozycjeoferty WHERE ID_oferta = @OfferId AND id_firmy = @CompanyId " +
            "ORDER BY nr_zespolu, ID_pozycja_oferty",
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
            "SELECT ID_pozycja_oferty, id_firmy, ID_oferta, id_towaru, id_dostawcy, kod_towaru, " +
            "Nazwa, Nazwa_ENG, jednostki, jednostki_en, Sztuki, Cena, Rabat, Cena_po_rabacie, " +
            "Cena_po_rabacie_i_sztukach, stawka_vat, vat, cena_brutto, Uwagi_oferta, uwagi_faktura, " +
            "inne1, nr_zespolu " +
            "FROM apozycjeoferty WHERE id_firmy = @CompanyId " +
            "ORDER BY ID_oferta, nr_zespolu, ID_pozycja_oferty",
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
            "INSERT INTO apozycjeoferty (id_firmy, ID_oferta, id_towaru, id_dostawcy, kod_towaru, " +
            "Nazwa, Nazwa_ENG, jednostki, jednostki_en, Sztuki, Cena, Rabat, Cena_po_rabacie, " +
            "Cena_po_rabacie_i_sztukach, stawka_vat, vat, cena_brutto, Uwagi_oferta, uwagi_faktura, " +
            "inne1, nr_zespolu) " +
            "VALUES (@CompanyId, @OfferId, @ProductId, @SupplierId, @ProductCode, @Name, @NameEng, " +
            "@Unit, @UnitEng, @Quantity, @Price, @Discount, @PriceAfterDiscount, " +
            "@PriceAfterDiscountAndQuantity, @VatRate, @Vat, @PriceBrutto, @OfferNotes, @InvoiceNotes, " +
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
            "UPDATE apozycjeoferty SET " +
            "ID_oferta = @OfferId, id_towaru = @ProductId, id_dostawcy = @SupplierId, kod_towaru = @ProductCode, " +
            "Nazwa = @Name, Nazwa_ENG = @NameEng, jednostki = @Unit, jednostki_en = @UnitEng, " +
            "Sztuki = @Quantity, Cena = @Price, Rabat = @Discount, Cena_po_rabacie = @PriceAfterDiscount, " +
            "Cena_po_rabacie_i_sztukach = @PriceAfterDiscountAndQuantity, stawka_vat = @VatRate, " +
            "vat = @Vat, cena_brutto = @PriceBrutto, Uwagi_oferta = @OfferNotes, uwagi_faktura = @InvoiceNotes, " +
            "inne1 = @Other1, nr_zespolu = @GroupNumber " +
            "WHERE ID_pozycja_oferty = @Id AND id_firmy = @CompanyId",
            connection);
        
        command.Parameters.AddWithValue("@Id", offerPosition.Id);
        AddOfferPositionParameters(command, offerPosition);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM apozycjeoferty WHERE ID_pozycja_oferty = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteByOfferIdAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM apozycjeoferty WHERE ID_oferta = @OfferId AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@OfferId", offerId);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM apozycjeoferty WHERE ID_pozycja_oferty = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private int GetCurrentCompanyId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException("Brak kontekstu HTTP. Metoda musi być wywołana w kontekście requestu HTTP.");

        var companyId = httpContext.User.GetCompanyId();
        if (!companyId.HasValue)
            throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
        return companyId.Value;
    }

    private static OfferPosition MapToOfferPosition(MySqlDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("ID_pozycja_oferty"));
        int companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        int? offerId = GetNullableInt(reader, "ID_oferta") ?? 0; // Jeśli NULL, ustawiamy 0, ale powinno być ustawione
        string unit = reader.GetString(reader.GetOrdinal("jednostki"));
        
        var position = new OfferPosition(companyId, offerId.Value, unit);
        
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
            GetNullableDecimal(reader, "Sztuki"),
            GetNullableDecimal(reader, "Cena"),
            GetNullableDecimal(reader, "Rabat"),
            GetNullableDecimal(reader, "Cena_po_rabacie"),
            GetNullableDecimal(reader, "Cena_po_rabacie_i_sztukach")
        );
        
        position.UpdateVatInfo(
            GetNullableString(reader, "stawka_vat"),
            GetNullableDecimal(reader, "vat"),
            GetNullableDecimal(reader, "cena_brutto")
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
        command.Parameters.AddWithValue("@Quantity", offerPosition.Quantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Price", offerPosition.Price ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Discount", offerPosition.Discount ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PriceAfterDiscount", offerPosition.PriceAfterDiscount ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PriceAfterDiscountAndQuantity", offerPosition.PriceAfterDiscountAndQuantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatRate", offerPosition.VatRate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Vat", offerPosition.Vat ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PriceBrutto", offerPosition.PriceBrutto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferNotes", offerPosition.OfferNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@InvoiceNotes", offerPosition.InvoiceNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Other1", offerPosition.Other1 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@GroupNumber", offerPosition.GroupNumber ?? (object)DBNull.Value);
    }
}
