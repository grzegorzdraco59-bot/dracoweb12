using ERP.Application.Services;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Dostawcy (Supplier) używająca MySqlConnector
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
            "SELECT id_dostawcy, id_firmy, nazwa_dostawcy, waluta, mail, telefon, uwagi " +
            "FROM dostawcy WHERE id_dostawcy = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "SELECT id_dostawcy, id_firmy, nazwa_dostawcy, waluta, mail, telefon, uwagi " +
            "FROM dostawcy WHERE id_firmy = @CompanyId ORDER BY nazwa_dostawcy",
            connection);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "SELECT id_dostawcy, id_firmy, nazwa_dostawcy, waluta, mail, telefon, uwagi " +
            "FROM dostawcy WHERE nazwa_dostawcy = @Name AND id_firmy = @CompanyId LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@Name", name);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
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
            "INSERT INTO dostawcy (id_firmy, nazwa_dostawcy, waluta, mail, telefon, uwagi) " +
            "VALUES (@CompanyId, @Name, @Currency, @Email, @Phone, @Notes); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        command.Parameters.AddWithValue("@CompanyId", supplier.CompanyId);
        command.Parameters.AddWithValue("@Name", supplier.Name);
        command.Parameters.AddWithValue("@Currency", supplier.Currency);
        command.Parameters.AddWithValue("@Email", supplier.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Phone", supplier.Phone);
        command.Parameters.AddWithValue("@Notes", supplier.Notes ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE dostawcy SET " +
            "id_firmy = @CompanyId, nazwa_dostawcy = @Name, waluta = @Currency, " +
            "mail = @Email, telefon = @Phone, uwagi = @Notes " +
            "WHERE id_dostawcy = @Id AND id_firmy = @CompanyId",
            connection);

        command.Parameters.AddWithValue("@Id", supplier.Id);
        command.Parameters.AddWithValue("@CompanyId", supplier.CompanyId);
        command.Parameters.AddWithValue("@Name", supplier.Name);
        command.Parameters.AddWithValue("@Currency", supplier.Currency);
        command.Parameters.AddWithValue("@Email", supplier.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Phone", supplier.Phone);
        command.Parameters.AddWithValue("@Notes", supplier.Notes ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM dostawcy WHERE id_dostawcy = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM dostawcy WHERE id_dostawcy = @Id AND id_firmy = @CompanyId",
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

    private static Supplier MapToSupplier(MySqlDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("id_dostawcy"));
        int companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        string name = reader.GetString(reader.GetOrdinal("nazwa_dostawcy"));
        string currency = reader.GetString(reader.GetOrdinal("waluta"));
        string phone = reader.GetString(reader.GetOrdinal("telefon"));
        
        string? email = GetNullableString(reader, "mail");
        string? notes = GetNullableString(reader, "uwagi");
        
        var supplier = SupplierFactory.FromDatabase(id, companyId, name, phone, currency, email, notes);
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
