using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium OrderPositionMain (pozyjezamowienia) używająca MySqlConnector
/// </summary>
public class OrderPositionMainRepository : IOrderPositionMainRepository
{
    private readonly DatabaseContext _context;

    public OrderPositionMainRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<OrderPositionMainDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT * FROM pozycjezamowienia WHERE id_pozycji_zamowienia = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToDto(reader);
        }

        return null;
    }

    public async Task<IEnumerable<OrderPositionMainDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var positions = new List<OrderPositionMainDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT * FROM pozycjezamowienia ORDER BY id_zamowienia, id_pozycji_zamowienia",
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            positions.Add(MapToDto(reader));
        }

        return positions;
    }

    public async Task<IEnumerable<OrderPositionMainDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var positions = new List<OrderPositionMainDto>();
        try
        {
            await using var connection = await _context.CreateConnectionAsync();
            
            // Połączenie przez zam.id_zamowienia = pozzam.id_zamowienia
            // Używamy id_zamowienia zgodnie ze strukturą bazy danych
            var command = new MySqlCommand(
                "SELECT * FROM pozycjezamowienia WHERE id_zamowienia = @OrderId",
                connection);
            command.Parameters.AddWithValue("@OrderId", orderId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    var position = MapToDto(reader);
                    positions.Add(position);
                }
                catch (Exception ex)
                {
                    // Loguj błąd mapowania, ale kontynuuj z następnym rekordem
                    System.Diagnostics.Debug.WriteLine($"Błąd mapowania pozycji zamówienia: {ex.Message}");
                }
            }
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1146)
        {
            System.Diagnostics.Debug.WriteLine($"Tabela 'pozycjezamowienia' nie istnieje: {ex.Message}");
            // Rzuć wyjątek, żeby ViewModel mógł go obsłużyć
            throw new InvalidOperationException($"Tabela 'pozycjezamowienia' nie istnieje w bazie danych. Sprawdź czy tabela została utworzona.", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd podczas ładowania pozycji zamówienia: {ex.Message}\n{ex.StackTrace}");
            // Rzuć wyjątek, żeby ViewModel mógł go obsłużyć
            throw;
        }

        return positions;
    }

    public async Task<int?> GetOrderIdLinkedToOfferAsync(int offerId, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT p.id_zamowienia FROM pozycjezamowienia p " +
            "INNER JOIN ofertypozycje a ON a.ID_pozycja_oferty = p.id_pozycji_pozycji_oferty AND a.id_firmy = @CompanyId " +
            "WHERE a.oferta_id = @OfferId AND p.id_firmy = @CompanyId " +
            "LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@OfferId", offerId);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result == null || result == DBNull.Value) return null;
        return Convert.ToInt32(result);
    }

    public async Task<int> AddAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO pozycjezamowienia (id_firmy, id_zamowienia, id_towaru, data_dostawy_pozycji, " +
            "towar_nazwa_draco, towar, towar_nazwa_ENG, jednostki_zamawiane, ilosc_zamawiana, ilosc_dostarczona, " +
            "cena_zamawiana, status_towaru, jednostki_zakupu, ilosc_zakupu, cena_zakupu, wartsc_zakupu, " +
            "cena_zakupu_pln, przelicznik_m_kg, cena_zakupu_PLN_nowe_jednostki, uwagi, dostawca_pozycji, " +
            "stawka_vat, ciezar_jednostkowy, ilosc_w_opakowaniu, id_zamowienia_hala, id_pozycji_pozycji_oferty, " +
            "zaznacz_do_kopiowania, skopiowano_do_magazynu, dlugosc) " +
            "VALUES (@CompanyId, @OrderId, @ProductId, @DeliveryDateInt, @ProductNameDraco, @Product, " +
            "@ProductNameEng, @OrderUnit, @OrderQuantity, @DeliveredQuantity, @OrderPrice, @ProductStatus, " +
            "@PurchaseUnit, @PurchaseQuantity, @PurchasePrice, @PurchaseValue, @PurchasePricePln, " +
            "@ConversionFactor, @PurchasePricePlnNewUnit, @Notes, @Supplier, @VatRate, @UnitWeight, " +
            "@QuantityInPackage, @OrderHalaId, @OfferPositionId, @MarkForCopying, @CopiedToWarehouse, @Length); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@CompanyId", position.CompanyId);
        command.Parameters.AddWithValue("@OrderId", position.OrderId);
        command.Parameters.AddWithValue("@ProductId", position.ProductId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DeliveryDateInt", position.DeliveryDateInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductNameDraco", position.ProductNameDraco ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Product", position.Product ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductNameEng", position.ProductNameEng ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderUnit", position.OrderUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderQuantity", position.OrderQuantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DeliveredQuantity", position.DeliveredQuantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderPrice", position.OrderPrice ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductStatus", position.ProductStatus ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchaseUnit", position.PurchaseUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchaseQuantity", position.PurchaseQuantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchasePrice", position.PurchasePrice ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchaseValue", position.PurchaseValue ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchasePricePln", position.PurchasePricePln ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ConversionFactor", position.ConversionFactor ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchasePricePlnNewUnit", position.PurchasePricePlnNewUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", position.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Supplier", position.Supplier ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatRate", position.VatRate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@UnitWeight", position.UnitWeight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@QuantityInPackage", position.QuantityInPackage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderHalaId", position.OrderHalaId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferPositionId", position.OfferPositionId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@MarkForCopying", position.MarkForCopying ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CopiedToWarehouse", position.CopiedToWarehouse ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Length", position.Length ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(OrderPositionMainDto position, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE pozycjezamowienia SET " +
            "id_firmy = @CompanyId, id_zamowienia = @OrderId, id_towaru = @ProductId, " +
            "data_dostawy_pozycji = @DeliveryDateInt, towar_nazwa_draco = @ProductNameDraco, " +
            "towar = @Product, towar_nazwa_ENG = @ProductNameEng, jednostki_zamawiane = @OrderUnit, " +
            "ilosc_zamawiana = @OrderQuantity, ilosc_dostarczona = @DeliveredQuantity, " +
            "cena_zamawiana = @OrderPrice, status_towaru = @ProductStatus, jednostki_zakupu = @PurchaseUnit, " +
            "ilosc_zakupu = @PurchaseQuantity, cena_zakupu = @PurchasePrice, wartsc_zakupu = @PurchaseValue, " +
            "cena_zakupu_pln = @PurchasePricePln, przelicznik_m_kg = @ConversionFactor, " +
            "cena_zakupu_PLN_nowe_jednostki = @PurchasePricePlnNewUnit, uwagi = @Notes, " +
            "dostawca_pozycji = @Supplier, stawka_vat = @VatRate, ciezar_jednostkowy = @UnitWeight, " +
            "ilosc_w_opakowaniu = @QuantityInPackage, id_zamowienia_hala = @OrderHalaId, " +
            "id_pozycji_pozycji_oferty = @OfferPositionId, zaznacz_do_kopiowania = @MarkForCopying, " +
            "skopiowano_do_magazynu = @CopiedToWarehouse, dlugosc = @Length " +
            "WHERE id_pozycji_zamowienia = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", position.Id);
        command.Parameters.AddWithValue("@CompanyId", position.CompanyId);
        command.Parameters.AddWithValue("@OrderId", position.OrderId);
        command.Parameters.AddWithValue("@ProductId", position.ProductId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DeliveryDateInt", position.DeliveryDateInt ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductNameDraco", position.ProductNameDraco ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Product", position.Product ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductNameEng", position.ProductNameEng ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderUnit", position.OrderUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderQuantity", position.OrderQuantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DeliveredQuantity", position.DeliveredQuantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderPrice", position.OrderPrice ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductStatus", position.ProductStatus ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchaseUnit", position.PurchaseUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchaseQuantity", position.PurchaseQuantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchasePrice", position.PurchasePrice ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchaseValue", position.PurchaseValue ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchasePricePln", position.PurchasePricePln ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ConversionFactor", position.ConversionFactor ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchasePricePlnNewUnit", position.PurchasePricePlnNewUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", position.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Supplier", position.Supplier ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatRate", position.VatRate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@UnitWeight", position.UnitWeight ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@QuantityInPackage", position.QuantityInPackage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OrderHalaId", position.OrderHalaId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferPositionId", position.OfferPositionId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@MarkForCopying", position.MarkForCopying ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CopiedToWarehouse", position.CopiedToWarehouse ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Length", position.Length ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM pozycjezamowienia WHERE id_pozycji_zamowienia = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static OrderPositionMainDto MapToDto(MySqlDataReader reader)
    {
        // Najpierw sprawdźmy jakie kolumny są dostępne
        var availableColumns = new HashSet<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            availableColumns.Add(reader.GetName(i));
        }

        // Sprawdźmy nazwę kolumny ID - może być id_pozycji_zamowienia, id, id_pozycji, ID_pozycji itp.
        string? idColumnName = null;
        if (availableColumns.Contains("id_pozycji_zamowienia"))
            idColumnName = "id_pozycji_zamowienia";
        else if (availableColumns.Contains("id"))
            idColumnName = "id";
        else if (availableColumns.Contains("id_pozycji"))
            idColumnName = "id_pozycji";
        else if (availableColumns.Contains("ID_pozycji"))
            idColumnName = "ID_pozycji";
        else if (availableColumns.Contains("ID"))
            idColumnName = "ID";

        // Helper do znajdowania nazwy kolumny z różnymi wariantami
        string? FindColumn(HashSet<string> columns, params string[] names)
        {
            foreach (var name in names)
            {
                if (columns.Contains(name))
                    return name;
            }
            return null;
        }

        var idCol = FindColumn(availableColumns, "id_pozycji_zamowienia", "id", "ID_pozycji_zamowienia", "ID");
        var companyIdCol = FindColumn(availableColumns, "id_firmy", "ID_firmy");
        var orderIdCol = FindColumn(availableColumns, "id_zamowienia", "ID_zamowienia");
        var productIdCol = FindColumn(availableColumns, "id_towaru", "ID_towaru");
        var deliveryDateCol = FindColumn(availableColumns, "data_dostawy_pozycji", "DATA_dostawy_pozycji");
        
        // Konwersja daty z formatu Clarion (int) na DateTime
        int? deliveryDateInt = GetNullableInt(reader, deliveryDateCol);
        DateTime? deliveryDate = null;
        if (deliveryDateInt.HasValue && deliveryDateInt.Value > 0)
        {
            try
            {
                // Format Clarion: dni od 28.12.1800
                var clarionEpoch = new DateTime(1800, 12, 28);
                deliveryDate = clarionEpoch.AddDays(deliveryDateInt.Value);
            }
            catch
            {
                // Jeśli konwersja się nie powiedzie, pozostaw null
            }
        }

        return new OrderPositionMainDto
        {
            Id = GetInt(reader, idCol ?? idColumnName),
            CompanyId = GetInt(reader, companyIdCol),
            OrderId = GetInt(reader, orderIdCol),
            ProductId = GetNullableInt(reader, productIdCol),
            DeliveryDateInt = deliveryDateInt,
            DeliveryDate = deliveryDate,
            ProductNameDraco = GetNullableString(reader, FindColumn(availableColumns, "towar_nazwa_draco", "TOWAR_nazwa_draco")),
            Product = GetNullableString(reader, FindColumn(availableColumns, "towar", "TOWAR")),
            ProductNameEng = GetNullableString(reader, FindColumn(availableColumns, "towar_nazwa_ENG", "towar_nazwa_ENG", "TOWAR_nazwa_ENG")),
            OrderUnit = GetNullableString(reader, FindColumn(availableColumns, "jednostki_zamawiane", "JEDNOSTKI_zamawiane")),
            OrderQuantity = GetNullableDecimal(reader, FindColumn(availableColumns, "ilosc_zamawiana", "ILOSC_zamawiana")),
            DeliveredQuantity = GetNullableDecimal(reader, FindColumn(availableColumns, "ilosc_dostarczona", "ILOSC_dostarczona")),
            OrderPrice = GetNullableDecimal(reader, FindColumn(availableColumns, "cena_zamawiana", "CENA_zamawiana")),
            ProductStatus = GetNullableString(reader, FindColumn(availableColumns, "status_towaru", "STATUS_towaru")),
            PurchaseUnit = GetNullableString(reader, FindColumn(availableColumns, "jednostki_zakupu", "JEDNOSTKI_zakupu")),
            PurchaseQuantity = GetNullableDecimal(reader, FindColumn(availableColumns, "ilosc_zakupu", "ILOSC_zakupu")),
            PurchasePrice = GetNullableDecimal(reader, FindColumn(availableColumns, "cena_zakupu", "CENA_zakupu")),
            PurchaseValue = GetNullableDecimal(reader, FindColumn(availableColumns, "wartsc_zakupu", "WARTSC_zakupu")),
            PurchasePricePln = GetNullableDecimal(reader, FindColumn(availableColumns, "cena_zakupu_pln", "CENA_zakupu_pln")),
            ConversionFactor = GetNullableDecimal(reader, FindColumn(availableColumns, "przelicznik_m_kg", "PRZELICZNIK_m_kg")),
            PurchasePricePlnNewUnit = GetNullableDecimal(reader, FindColumn(availableColumns, "cena_zakupu_PLN_nowe_jednostki", "CENA_zakupu_PLN_nowe_jednostki")),
            Notes = GetNullableString(reader, FindColumn(availableColumns, "uwagi", "UWAGI")),
            Supplier = GetNullableString(reader, FindColumn(availableColumns, "dostawca_pozycji", "DOSTAWCA_pozycji")),
            VatRate = GetNullableString(reader, FindColumn(availableColumns, "stawka_vat", "STAWKA_vat")),
            UnitWeight = GetNullableDecimal(reader, FindColumn(availableColumns, "ciezar_jednostkowy", "CIEZAR_jednostkowy")),
            QuantityInPackage = GetNullableDecimal(reader, FindColumn(availableColumns, "ilosc_w_opakowaniu", "ILOSC_w_opakowaniu")),
            OrderHalaId = GetNullableInt(reader, FindColumn(availableColumns, "id_zamowienia_hala", "ID_zamowienia_hala")),
            OfferPositionId = GetNullableInt(reader, FindColumn(availableColumns, "id_pozycji_pozycji_oferty", "ID_pozycji_pozycji_oferty")),
            MarkForCopying = GetNullableInt(reader, FindColumn(availableColumns, "zaznacz_do_kopiowania", "ZAZNACZ_do_kopiowania")),
            CopiedToWarehouse = GetNullableBool(reader, FindColumn(availableColumns, "skopiowano_do_magazynu", "SKOPIOWANO_do_magazynu")),
            Length = GetNullableDecimal(reader, FindColumn(availableColumns, "dlugosc", "DLUGOSC")),
            CreatedAt = GetDateTime(reader, FindColumn(availableColumns, "CreatedAt", "created_at", "CREATED_AT")),
            UpdatedAt = GetNullableDateTime(reader, FindColumn(availableColumns, "UpdatedAt", "updated_at", "UPDATED_AT"))
        };
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

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return null;
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

    private static bool? GetNullableBool(MySqlDataReader reader, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return null;
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;
            return reader.GetBoolean(ordinal);
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
}
