using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium kontrahentów (Supplier) używająca MySqlConnector
/// </summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;

    public SupplierRepository(DatabaseContext context, IUserContext userContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, company_id, nazwa, waluta, email, telefon " +
            "FROM kontrahenci_v WHERE id = @Id AND company_id = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToSupplier(reader);
        }

        return null;
    }

    public async Task<Supplier?> GetByKontrahentIdAsync(int kontrahentId, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, company_id, nazwa, waluta, email, telefon " +
            "FROM kontrahenci_v WHERE id = @KontrahentId AND company_id = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@KontrahentId", kontrahentId);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToSupplier(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = new List<Supplier>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, company_id, nazwa, waluta, email, telefon " +
            "FROM kontrahenci_v WHERE company_id = @CompanyId ORDER BY nazwa",
            connection);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            suppliers.Add(MapToSupplier(reader));
        }

        return suppliers;
    }

    public async Task<Supplier?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id, company_id, nazwa, waluta, email, telefon " +
            "FROM kontrahenci_v WHERE nazwa = @Name AND company_id = @CompanyId LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToSupplier(reader);
        }

        return null;
    }

    public async Task<int> AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO kontrahenci (company_id, typ, nazwa, waluta, email, telefon) " +
            "VALUES (@CompanyId, @Typ, @Name, @Currency, @Email, @Phone); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@CompanyId", supplier.CompanyId);
        command.Parameters.AddWithValue("@Typ", DBNull.Value);
        command.Parameters.AddWithValue("@Name", supplier.Name);
        command.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(supplier.Currency) ? "PLN" : supplier.Currency);
        command.Parameters.AddWithValue("@Email", supplier.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Phone", supplier.Phone ?? string.Empty);

        var newId = await command.ExecuteInsertAndGetIdAsync(cancellationToken);
        return (int)newId;
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE kontrahenci SET " +
            "company_id = @CompanyId, typ = @Typ, nazwa = @Name, waluta = @Currency, " +
            "email = @Email, telefon = @Phone " +
            "WHERE id = @Id AND company_id = @CompanyId",
            connection);

        command.Parameters.AddWithValue("@Id", supplier.Id);
        command.Parameters.AddWithValue("@CompanyId", supplier.CompanyId);
        command.Parameters.AddWithValue("@Typ", DBNull.Value);
        command.Parameters.AddWithValue("@Name", supplier.Name);
        command.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(supplier.Currency) ? "PLN" : supplier.Currency);
        command.Parameters.AddWithValue("@Email", supplier.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Phone", supplier.Phone ?? string.Empty);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM kontrahenci WHERE id = @Id AND company_id = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM kontrahenci WHERE id = @Id AND company_id = @CompanyId",
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

    private static Supplier MapToSupplier(MySqlDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("id"));
        int companyId = reader.GetInt32(reader.GetOrdinal("company_id"));
        string name = reader.GetString(reader.GetOrdinal("nazwa"));
        string currency = GetNullableString(reader, "waluta") ?? "PLN";
        string phone = GetNullableString(reader, "telefon") ?? string.Empty;
        string? email = GetNullableString(reader, "email");

        var supplier = SupplierFactory.FromDatabase(id, companyId, name, phone, currency, email, null);
        return supplier;
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
}
