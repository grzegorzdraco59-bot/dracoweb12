using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Order (zamowieniahala) używająca MySqlConnector
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;

    public OrderRepository(DatabaseContext context, IUserContext userContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_zamowienia_hala, id_firmy, id_zamowienia, data_zamowienia, id_dostawcy, " +
            "dostawca, dostawca_mail, dostawca_waluta, id_towaru, nazwa_towaru_draco, " +
            "nazwa_towaru, status_towaru, jednostki_zakupu, jednostki_sprzedazy, cena_zakupu, " +
            "przelicznik_m_kg, ilosc, uwagi, zaznacz_do_zamowienia, wyslano_do_zamowienia, " +
            "dostarczono, ilosc_w_opakowaniu, stawka_vat, operator, nr_zam_skaner " +
            "FROM zamowieniahala WHERE id_zamowienia_hala = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToOrder(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orders = new List<Order>();
        try
        {
            await using var connection = await _context.CreateConnectionAsync();
            
            // Pobierz dane z tabeli zamowieniahala - wszystkie pola
            var command = new MySqlCommand(
                "SELECT id_zamowienia_hala, id_firmy, id_zamowienia, data_zamowienia, id_dostawcy, " +
                "dostawca, dostawca_mail, dostawca_waluta, id_towaru, nazwa_towaru_draco, " +
                "nazwa_towaru, status_towaru, jednostki_zakupu, jednostki_sprzedazy, cena_zakupu, " +
                "przelicznik_m_kg, ilosc, uwagi, zaznacz_do_zamowienia, wyslano_do_zamowienia, " +
                "dostarczono, ilosc_w_opakowaniu, stawka_vat, operator, nr_zam_skaner " +
                "FROM zamowieniahala " +
                "WHERE id_firmy = @CompanyId " +
                "ORDER BY data_zamowienia DESC, id_zamowienia_hala DESC",
                connection);
            command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    orders.Add(MapToOrder(reader));
                }
                catch (Exception ex)
                {
                    // Loguj błąd mapowania, ale kontynuuj przetwarzanie innych rekordów
                    System.Diagnostics.Debug.WriteLine($"Błąd mapowania zamówienia: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Błąd podczas pobierania zamówień: {ex.Message}", ex);
        }

        return orders;
    }

    public async Task<IEnumerable<Order>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var orders = new List<Order>();
        await using var connection = await _context.CreateConnectionAsync();
        
        var command = new MySqlCommand(
            "SELECT id_zamowienia_hala, id_firmy, id_zamowienia, data_zamowienia, id_dostawcy, " +
            "dostawca, dostawca_mail, dostawca_waluta, id_towaru, nazwa_towaru_draco, " +
            "nazwa_towaru, status_towaru, jednostki_zakupu, jednostki_sprzedazy, cena_zakupu, " +
            "przelicznik_m_kg, ilosc, uwagi, zaznacz_do_zamowienia, wyslano_do_zamowienia, " +
            "dostarczono, ilosc_w_opakowaniu, stawka_vat, operator, nr_zam_skaner " +
            "FROM zamowieniahala " +
            "WHERE id_firmy = @CompanyId " +
            "ORDER BY data_zamowienia DESC, id_zamowienia_hala DESC",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(MapToOrder(reader));
        }

        return orders;
    }

    public async Task<int> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO zamowieniahala (id_firmy, id_zamowienia, data_zamowienia, id_dostawcy, dostawca, " +
            "dostawca_mail, dostawca_waluta, id_towaru, nazwa_towaru_draco, nazwa_towaru, status_towaru, " +
            "jednostki_zakupu, jednostki_sprzedazy, cena_zakupu, przelicznik_m_kg, ilosc, uwagi, " +
            "zaznacz_do_zamowienia, wyslano_do_zamowienia, dostarczono, ilosc_w_opakowaniu, stawka_vat, " +
            "operator, nr_zam_skaner) " +
            "VALUES (@CompanyId, @OrderNumber, @OrderDate, @SupplierId, @SupplierName, @SupplierEmail, " +
            "@SupplierCurrency, @ProductId, @ProductNameDraco, @ProductName, @ProductStatus, @PurchaseUnit, " +
            "@SalesUnit, @PurchasePrice, @ConversionFactor, @Quantity, @Notes, @Status, @SentToOrder, " +
            "@Delivered, @QuantityInPackage, @VatRate, @Operator, @ScannerOrderNumber); " +
            "SELECT LAST_INSERT_ID();",
            connection);
        
        AddOrderParameters(command, order);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE zamowieniahala SET " +
            "id_firmy = @CompanyId, id_zamowienia = @OrderNumber, data_zamowienia = @OrderDate, " +
            "id_dostawcy = @SupplierId, dostawca = @SupplierName, dostawca_mail = @SupplierEmail, " +
            "dostawca_waluta = @SupplierCurrency, id_towaru = @ProductId, nazwa_towaru_draco = @ProductNameDraco, " +
            "nazwa_towaru = @ProductName, status_towaru = @ProductStatus, jednostki_zakupu = @PurchaseUnit, " +
            "jednostki_sprzedazy = @SalesUnit, cena_zakupu = @PurchasePrice, przelicznik_m_kg = @ConversionFactor, " +
            "ilosc = @Quantity, uwagi = @Notes, zaznacz_do_zamowienia = @Status, " +
            "wyslano_do_zamowienia = @SentToOrder, dostarczono = @Delivered, ilosc_w_opakowaniu = @QuantityInPackage, " +
            "stawka_vat = @VatRate, operator = @Operator, nr_zam_skaner = @ScannerOrderNumber " +
            "WHERE id_zamowienia_hala = @Id",
            connection);
        
        command.Parameters.AddWithValue("@Id", order.Id);
        AddOrderParameters(command, order);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM zamowieniahala WHERE id_zamowienia_hala = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Order MapToOrder(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id_zamowienia_hala"));
        var companyId = GetNullableInt(reader, "id_firmy") ?? 0;
        var orderNumberInt = GetNullableInt(reader, "id_zamowienia");
        var dataZamowieniaInt = GetNullableInt(reader, "data_zamowienia");
        var supplierId = GetNullableInt(reader, "id_dostawcy");
        var supplierName = GetNullableString(reader, "dostawca");
        var supplierEmail = GetNullableString(reader, "dostawca_mail");
        var supplierCurrency = GetNullableString(reader, "dostawca_waluta");
        var productId = GetNullableInt(reader, "id_towaru");
        var productNameDraco = GetNullableString(reader, "nazwa_towaru_draco");
        var productName = GetNullableString(reader, "nazwa_towaru");
        var productStatus = GetNullableString(reader, "status_towaru");
        var purchaseUnit = GetNullableString(reader, "jednostki_zakupu");
        var salesUnit = GetNullableString(reader, "jednostki_sprzedazy");
        var purchasePrice = GetNullableDecimal(reader, "cena_zakupu");
        var conversionFactor = GetNullableDecimal(reader, "przelicznik_m_kg");
        var quantity = GetNullableDecimal(reader, "ilosc");
        var notes = GetNullableString(reader, "uwagi");
        var status = GetNullableInt(reader, "zaznacz_do_zamowienia");
        var sentToOrder = GetNullableBool(reader, "wyslano_do_zamowienia");
        var delivered = GetNullableBool(reader, "dostarczono");
        var quantityInPackage = GetNullableDecimal(reader, "ilosc_w_opakowaniu");
        var vatRate = GetNullableString(reader, "stawka_vat");
        var operatorName = GetNullableString(reader, "operator");
        var scannerOrderNumber = GetNullableInt(reader, "nr_zam_skaner");
        
        // Konwersja data_zamowienia z int na DateTime (format Clarion 11: liczba dni od 1800-12-28)
        DateTime? orderDate = null;
        if (dataZamowieniaInt.HasValue)
        {
            orderDate = new DateTime(1800, 12, 28).AddDays(dataZamowieniaInt.Value);
        }
        
        var order = new Order(companyId);
        
        // Używamy refleksji do ustawienia Id
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProperty?.SetValue(order, id);
        
        // Ustawiamy wszystkie pola
        order.OrderNumberInt = orderNumberInt;
        order.OrderDateInt = dataZamowieniaInt;
        order.OrderDate = orderDate;
        order.UpdateSupplier(supplierId, supplierName, supplierEmail, supplierCurrency);
        order.UpdateProductInfo(productId, productNameDraco, productName, productStatus);
        order.UpdatePricing(purchasePrice, quantity, conversionFactor, quantityInPackage, purchaseUnit, salesUnit);
        order.UpdateStatus(status);
        order.UpdateDeliveryInfo(sentToOrder, delivered);
        order.UpdateVatAndOperator(vatRate, operatorName, scannerOrderNumber);
        
        // Ustawiamy Notes
        var notesProperty = typeof(Order).GetProperty("Notes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        notesProperty?.SetValue(order, notes);
        
        // Ustawiamy CreatedAt i UpdatedAt
        var createdAt = DateTime.Now;
        var createdAtProperty = typeof(BaseEntity).GetProperty("CreatedAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        createdAtProperty?.SetValue(order, createdAt);
        
        return order;
    }
    
    private static bool? GetNullableBool(MySqlDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;
            
            // Może być BIT(1) - zwraca jako byte lub bool
            var value = reader.GetValue(ordinal);
            if (value is bool boolValue)
                return boolValue;
            if (value is byte byteValue)
                return byteValue != 0;
            if (value is int intValue)
                return intValue != 0;
            
            return Convert.ToBoolean(value);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private static int? GetNullableInt(MySqlDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? GetNullableDateTime(MySqlDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        try
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }
        catch
        {
            return null;
        }
    }

    private static void AddOrderParameters(MySqlCommand command, Order order)
    {
        command.Parameters.AddWithValue("@CompanyId", order.CompanyId);
        command.Parameters.AddWithValue("@OrderNumber", order.OrderNumberInt ?? (object)DBNull.Value);
        
        // Konwersja DateTime na int (format Clarion 11 - liczba dni od 1800-12-28)
        if (order.OrderDateInt.HasValue)
        {
            command.Parameters.AddWithValue("@OrderDate", order.OrderDateInt.Value);
        }
        else if (order.OrderDate.HasValue)
        {
            var days = (int)(order.OrderDate.Value - new DateTime(1800, 12, 28)).TotalDays;
            command.Parameters.AddWithValue("@OrderDate", days);
        }
        else
        {
            command.Parameters.AddWithValue("@OrderDate", DBNull.Value);
        }
        
        command.Parameters.AddWithValue("@SupplierId", order.SupplierId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierName", order.SupplierName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierEmail", order.SupplierEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SupplierCurrency", order.SupplierCurrency ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductId", order.ProductId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductNameDraco", order.ProductNameDraco ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductName", order.ProductName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ProductStatus", order.ProductStatus ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchaseUnit", order.PurchaseUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SalesUnit", order.SalesUnit ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PurchasePrice", order.PurchasePrice ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ConversionFactor", order.ConversionFactor ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Quantity", order.Quantity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", order.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Status", order.Status ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SentToOrder", order.SentToOrder ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Delivered", order.Delivered ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@QuantityInPackage", order.QuantityInPackage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatRate", order.VatRate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Operator", order.Operator ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ScannerOrderNumber", order.ScannerOrderNumber ?? (object)DBNull.Value);
    }

    private int GetCurrentCompanyId()
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
        return companyId.Value;
    }
}
