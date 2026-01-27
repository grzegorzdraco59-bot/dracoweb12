using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Odbiorcy (Customer) używająca MySqlConnector
/// Repozytoria zawierają tylko operacje CRUD - logika biznesowa jest w warstwie Application
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly DatabaseContext _context;

    public CustomerRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Customer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_odbiorcy, id_firmy, Nazwa, Nazwisko, Imie, Uwagi, Tel_1, Tel_2, NIP, " +
            "Ulica_nr, Kod_pocztowy, Miasto, Kraj, Ulica_nr_wysylka, Kod_pocztowy_wysylka, " +
            "Miasto_wysylka, Kraj_wysylka, Email_1, Email_2, kod, status, waluta, odbiorca_typ, " +
            "do_oferty, status_vat, regon, adres_caly " +
            "FROM Odbiorcy WHERE ID_odbiorcy = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToCustomer(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Customer>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var customers = new List<Customer>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_odbiorcy, id_firmy, Nazwa, Nazwisko, Imie, Uwagi, Tel_1, Tel_2, NIP, " +
            "Ulica_nr, Kod_pocztowy, Miasto, Kraj, Ulica_nr_wysylka, Kod_pocztowy_wysylka, " +
            "Miasto_wysylka, Kraj_wysylka, Email_1, Email_2, kod, status, waluta, odbiorca_typ, " +
            "do_oferty, status_vat, regon, adres_caly " +
            "FROM Odbiorcy WHERE id_firmy = @CompanyId ORDER BY Nazwa",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            customers.Add(MapToCustomer(reader));
        }

        return customers;
    }

    public async Task<IEnumerable<Customer>> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var customers = new List<Customer>();
        await using var connection = await _context.CreateConnectionAsync();
        // Używamy status != 'nieaktywny' jako kryterium aktywności
        var command = new MySqlCommand(
            "SELECT ID_odbiorcy, id_firmy, Nazwa, Nazwisko, Imie, Uwagi, Tel_1, Tel_2, NIP, " +
            "Ulica_nr, Kod_pocztowy, Miasto, Kraj, Ulica_nr_wysylka, Kod_pocztowy_wysylka, " +
            "Miasto_wysylka, Kraj_wysylka, Email_1, Email_2, kod, status, waluta, odbiorca_typ, " +
            "do_oferty, status_vat, regon, adres_caly " +
            "FROM Odbiorcy WHERE id_firmy = @CompanyId AND (status IS NULL OR status != 'nieaktywny') ORDER BY Nazwa",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            customers.Add(MapToCustomer(reader));
        }

        return customers;
    }

    public async Task<Customer?> GetByNameAsync(string name, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_odbiorcy, id_firmy, Nazwa, Nazwisko, Imie, Uwagi, Tel_1, Tel_2, NIP, " +
            "Ulica_nr, Kod_pocztowy, Miasto, Kraj, Ulica_nr_wysylka, Kod_pocztowy_wysylka, " +
            "Miasto_wysylka, Kraj_wysylka, Email_1, Email_2, kod, status, waluta, odbiorca_typ, " +
            "do_oferty, status_vat, regon, adres_caly " +
            "FROM Odbiorcy WHERE Nazwa = @Name AND id_firmy = @CompanyId LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToCustomer(reader);
        }

        return null;
    }

    public async Task<int> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO Odbiorcy (id_firmy, Nazwa, Nazwisko, Imie, Uwagi, Tel_1, Tel_2, NIP, " +
            "Ulica_nr, Kod_pocztowy, Miasto, Kraj, Ulica_nr_wysylka, Kod_pocztowy_wysylka, " +
            "Miasto_wysylka, Kraj_wysylka, Email_1, Email_2, kod, status, waluta, odbiorca_typ, " +
            "do_oferty, status_vat, regon, adres_caly) " +
            "VALUES (@CompanyId, @Name, @Surname, @FirstName, @Notes, @Phone1, @Phone2, @Nip, " +
            "@Street, @PostalCode, @City, @Country, @ShippingStreet, @ShippingPostalCode, " +
            "@ShippingCity, @ShippingCountry, @Email1, @Email2, @Code, @Status, @Currency, " +
            "@CustomerType, @OfferEnabled, @VatStatus, @Regon, @FullAddress); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        AddCustomerParameters(command, customer);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE Odbiorcy SET id_firmy = @CompanyId, Nazwa = @Name, Nazwisko = @Surname, " +
            "Imie = @FirstName, Uwagi = @Notes, Tel_1 = @Phone1, Tel_2 = @Phone2, NIP = @Nip, " +
            "Ulica_nr = @Street, Kod_pocztowy = @PostalCode, Miasto = @City, Kraj = @Country, " +
            "Ulica_nr_wysylka = @ShippingStreet, Kod_pocztowy_wysylka = @ShippingPostalCode, " +
            "Miasto_wysylka = @ShippingCity, Kraj_wysylka = @ShippingCountry, " +
            "Email_1 = @Email1, Email_2 = @Email2, kod = @Code, status = @Status, waluta = @Currency, " +
            "odbiorca_typ = @CustomerType, do_oferty = @OfferEnabled, status_vat = @VatStatus, " +
            "regon = @Regon, adres_caly = @FullAddress " +
            "WHERE ID_odbiorcy = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", customer.Id);
        AddCustomerParameters(command, customer);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("DELETE FROM Odbiorcy WHERE ID_odbiorcy = @Id AND id_firmy = @CompanyId", connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("SELECT COUNT(1) FROM Odbiorcy WHERE ID_odbiorcy = @Id AND id_firmy = @CompanyId", connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static Customer MapToCustomer(MySqlDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("ID_odbiorcy"));
        int companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        string name = reader.GetString(reader.GetOrdinal("Nazwa"));
        
        string? surname = GetNullableString(reader, "Nazwisko");
        string? firstName = GetNullableString(reader, "Imie");
        string? notes = GetNullableString(reader, "Uwagi");
        string? phone1 = GetNullableString(reader, "Tel_1");
        string? phone2 = GetNullableString(reader, "Tel_2");
        string? nip = GetNullableString(reader, "NIP");
        string? street = GetNullableString(reader, "Ulica_nr");
        string? postalCode = GetNullableString(reader, "Kod_pocztowy");
        string? city = GetNullableString(reader, "Miasto");
        string? country = GetNullableString(reader, "Kraj");
        string? shippingStreet = GetNullableString(reader, "Ulica_nr_wysylka");
        string? shippingPostalCode = GetNullableString(reader, "Kod_pocztowy_wysylka");
        string? shippingCity = GetNullableString(reader, "Miasto_wysylka");
        string? shippingCountry = GetNullableString(reader, "Kraj_wysylka");
        string? email1 = GetNullableString(reader, "Email_1");
        string? email2 = GetNullableString(reader, "Email_2");
        string? code = GetNullableString(reader, "kod");
        string? status = GetNullableString(reader, "status");
        string currency = reader.GetString(reader.GetOrdinal("waluta"));
        int? customerType = GetNullableInt(reader, "odbiorca_typ");
        bool? offerEnabled = GetNullableBool(reader, "do_oferty");
        string? vatStatus = GetNullableString(reader, "status_vat");
        string? regon = GetNullableString(reader, "regon");
        string? fullAddress = GetNullableString(reader, "adres_caly");
        
        return CustomerFactory.FromDatabase(
            id, companyId, name, surname, firstName, notes, phone1, phone2, nip,
            street, postalCode, city, country, shippingStreet, shippingPostalCode,
            shippingCity, shippingCountry, email1, email2, code, status, currency,
            customerType, offerEnabled, vatStatus, regon, fullAddress);
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

    private static void AddCustomerParameters(MySqlCommand command, Customer customer)
    {
        command.Parameters.AddWithValue("@CompanyId", customer.CompanyId);
        command.Parameters.AddWithValue("@Name", customer.Name);
        command.Parameters.AddWithValue("@Surname", customer.Surname ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@FirstName", customer.FirstName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Notes", customer.Notes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Phone1", customer.Phone1 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Phone2", customer.Phone2 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nip", customer.Nip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Street", customer.Street ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PostalCode", customer.PostalCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@City", customer.City ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Country", customer.Country ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ShippingStreet", customer.ShippingStreet ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ShippingPostalCode", customer.ShippingPostalCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ShippingCity", customer.ShippingCity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ShippingCountry", customer.ShippingCountry ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Email1", customer.Email1 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Email2", customer.Email2 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Code", customer.Code ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Status", customer.Status ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Currency", customer.Currency);
        command.Parameters.AddWithValue("@CustomerType", customer.CustomerType ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferEnabled", customer.OfferEnabled ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatStatus", customer.VatStatus ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Regon", customer.Regon ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@FullAddress", customer.FullAddress ?? (object)DBNull.Value);
    }
}
