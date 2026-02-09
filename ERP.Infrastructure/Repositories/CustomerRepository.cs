using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium kontrahentów (Customer) używająca MySqlConnector
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
            "SELECT id, company_id, nazwa, email, telefon, miasto, waluta " +
            "FROM kontrahenci_v WHERE id = @Id AND company_id = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToCustomer(reader);
        }

        return null;
    }

    public async Task<Customer?> GetByKontrahentIdAsync(int kontrahentId, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, company_id, nazwa, email, telefon, miasto, waluta " +
            "FROM kontrahenci_v WHERE id = @KontrahentId AND company_id = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@KontrahentId", kontrahentId);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "SELECT id, company_id, nazwa, email, telefon, miasto, waluta " +
            "FROM kontrahenci_v WHERE company_id = @CompanyId ORDER BY nazwa",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
        var command = new MySqlCommand(
            "SELECT id, company_id, nazwa, email, telefon, miasto, waluta " +
            "FROM kontrahenci_v WHERE company_id = @CompanyId ORDER BY nazwa",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "SELECT id, company_id, nazwa, email, telefon, miasto, waluta " +
            "FROM kontrahenci_v WHERE nazwa = @Name AND company_id = @CompanyId LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "INSERT INTO kontrahenci (company_id, typ, nazwa, email, telefon, miasto, waluta) " +
            "VALUES (@CompanyId, @Typ, @Name, @Email, @Phone, @City, @Currency); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@CompanyId", customer.CompanyId);
        command.Parameters.AddWithValue("@Typ", DBNull.Value);
        command.Parameters.AddWithValue("@Name", customer.Name ?? string.Empty);
        command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(customer.Email1) ? (object)DBNull.Value : customer.Email1);
        command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(customer.Phone1) ? (object)DBNull.Value : customer.Phone1);
        command.Parameters.AddWithValue("@City", string.IsNullOrWhiteSpace(customer.City) ? (object)DBNull.Value : customer.City);
        command.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(customer.Currency) ? "PLN" : customer.Currency);
        var newId = await command.ExecuteInsertAndGetIdAsync(cancellationToken);
        return (int)newId;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE kontrahenci SET company_id = @CompanyId, typ = @Typ, nazwa = @Name, email = @Email, " +
            "telefon = @Phone, miasto = @City, waluta = @Currency " +
            "WHERE id = @Id AND company_id = @CompanyId",
            connection);

        command.Parameters.AddWithValue("@Id", customer.Id);
        command.Parameters.AddWithValue("@CompanyId", customer.CompanyId);
        command.Parameters.AddWithValue("@Typ", DBNull.Value);
        command.Parameters.AddWithValue("@Name", customer.Name ?? string.Empty);
        command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(customer.Email1) ? (object)DBNull.Value : customer.Email1);
        command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(customer.Phone1) ? (object)DBNull.Value : customer.Phone1);
        command.Parameters.AddWithValue("@City", string.IsNullOrWhiteSpace(customer.City) ? (object)DBNull.Value : customer.City);
        command.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(customer.Currency) ? "PLN" : customer.Currency);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("DELETE FROM kontrahenci WHERE id = @Id AND company_id = @CompanyId", connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("SELECT COUNT(1) FROM kontrahenci WHERE id = @Id AND company_id = @CompanyId", connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static Customer MapToCustomer(MySqlDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("id"));
        int companyId = reader.GetInt32(reader.GetOrdinal("company_id"));
        string name = reader.GetString(reader.GetOrdinal("nazwa"));
        string? email = GetNullableString(reader, "email");
        string? phone = GetNullableString(reader, "telefon");
        string? city = GetNullableString(reader, "miasto");
        string currency = GetNullableString(reader, "waluta") ?? "PLN";

        return CustomerFactory.FromDatabase(
            id, companyId, name, null, null, null, phone, null, null,
            null, null, city, null, null, null,
            null, null, email, null, null, null, currency,
            null, null, null, null, null);
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

}
