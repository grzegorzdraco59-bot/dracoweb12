using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Operacje zapisu dla kontrahentów – tabela bazowa kontrahenci.
/// </summary>
public class KontrahenciCommandRepository : IKontrahenciCommandRepository
{
    private readonly DatabaseContext _context;

    public KontrahenciCommandRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<KontrahentLookupDto?> GetByIdAsync(
        int companyId,
        int kontrahentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT id AS kontrahent_id, id, company_id, typ, nazwa, email, telefon, miasto, waluta " +
            "FROM kontrahenci " +
            "WHERE id = @Id AND company_id = @CompanyId",
            connection);
        cmd.Parameters.AddWithValue("@Id", kontrahentId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await using var reader = await cmd.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new KontrahentLookupDto
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            KontrahentId = reader.GetInt32(reader.GetOrdinal("kontrahent_id")),
            CompanyId = reader.IsDBNull(reader.GetOrdinal("company_id")) ? null : reader.GetInt32(reader.GetOrdinal("company_id")),
            Typ = reader.IsDBNull(reader.GetOrdinal("typ")) ? null : reader.GetString(reader.GetOrdinal("typ")),
            Nazwa = reader.IsDBNull(reader.GetOrdinal("nazwa")) ? null : reader.GetString(reader.GetOrdinal("nazwa")),
            Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
            Telefon = reader.IsDBNull(reader.GetOrdinal("telefon")) ? null : reader.GetString(reader.GetOrdinal("telefon")),
            Miasto = reader.IsDBNull(reader.GetOrdinal("miasto")) ? null : reader.GetString(reader.GetOrdinal("miasto")),
            Waluta = reader.IsDBNull(reader.GetOrdinal("waluta")) ? null : reader.GetString(reader.GetOrdinal("waluta"))
        };
    }

    public async Task<int> AddAsync(
        int companyId,
        string? typ,
        string? name,
        string? email,
        string? phone,
        string? city,
        string? currency,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var kontrahentId = await InsertKontrahentAsync(
            connection,
            companyId,
            typ,
            name,
            email,
            phone,
            city,
            currency,
            cancellationToken);
        return kontrahentId;
    }

    public async Task UpdateAsync(
        int companyId,
        int kontrahentId,
        string? typ,
        string? name,
        string? email,
        string? phone,
        string? city,
        string? currency,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "UPDATE kontrahenci SET " +
            "typ = @Typ, nazwa = @Name, email = @Email, telefon = @Phone, miasto = @City, waluta = @Currency " +
            "WHERE id = @Id AND company_id = @CompanyId",
            connection);
        cmd.Parameters.AddWithValue("@Typ", string.IsNullOrWhiteSpace(typ) ? (object)DBNull.Value : typ);
        cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? (object)DBNull.Value : name);
        cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
        cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
        cmd.Parameters.AddWithValue("@City", string.IsNullOrWhiteSpace(city) ? (object)DBNull.Value : city);
        cmd.Parameters.AddWithValue("@Currency", string.IsNullOrWhiteSpace(currency) ? (object)DBNull.Value : currency);
        cmd.Parameters.AddWithValue("@Id", kontrahentId);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> IsUsedInDocumentsAsync(
        int companyId,
        int kontrahentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();

        var offersCmd = new MySqlCommand(
            "SELECT COUNT(1) FROM aoferty WHERE odbiorca_ID_odbiorcy = @KontrahentId AND id_firmy = @CompanyId",
            connection);
        offersCmd.Parameters.AddWithValue("@KontrahentId", kontrahentId);
        offersCmd.Parameters.AddWithValue("@CompanyId", companyId);
        var offersCount = Convert.ToInt32(await offersCmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken));
        if (offersCount > 0) return true;

        var invoicesCmd = new MySqlCommand(
            "SELECT COUNT(1) FROM faktury WHERE id_odbiorca = @KontrahentId AND id_firmy = @CompanyId",
            connection);
        invoicesCmd.Parameters.AddWithValue("@KontrahentId", kontrahentId);
        invoicesCmd.Parameters.AddWithValue("@CompanyId", companyId);
        var invoicesCount = Convert.ToInt32(await invoicesCmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken));
        if (invoicesCount > 0) return true;

        var ordersCmd = new MySqlCommand(
            "SELECT COUNT(1) FROM zamowienia WHERE id_dostawcy = @KontrahentId AND id_firmy = @CompanyId",
            connection);
        ordersCmd.Parameters.AddWithValue("@KontrahentId", kontrahentId);
        ordersCmd.Parameters.AddWithValue("@CompanyId", companyId);
        var ordersCount = Convert.ToInt32(await ordersCmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken));
        return ordersCount > 0;
    }

    public async Task DeleteAsync(
        int companyId,
        int kontrahentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();

        var deleteKontrahent = new MySqlCommand(
            "DELETE FROM kontrahenci WHERE id = @KontrahentId AND company_id = @CompanyId",
            connection);
        deleteKontrahent.Parameters.AddWithValue("@KontrahentId", kontrahentId);
        deleteKontrahent.Parameters.AddWithValue("@CompanyId", companyId);
        await deleteKontrahent.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    private static async Task<int> InsertKontrahentAsync(
        MySqlConnection connection,
        int companyId,
        string? typ,
        string? name,
        string? email,
        string? phone,
        string? city,
        string? currency,
        CancellationToken cancellationToken)
    {
        var cmd = new MySqlCommand(
            "INSERT INTO kontrahenci (company_id, typ, nazwa, email, telefon, miasto, waluta) " +
            "VALUES (@CompanyId, @Typ, @Nazwa, @Email, @Telefon, @Miasto, @Waluta); " +
            "SELECT LAST_INSERT_ID();",
            connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        cmd.Parameters.AddWithValue("@Typ", string.IsNullOrWhiteSpace(typ) ? (object)DBNull.Value : typ);
        cmd.Parameters.AddWithValue("@Nazwa", name ?? "");
        cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
        cmd.Parameters.AddWithValue("@Telefon", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
        cmd.Parameters.AddWithValue("@Miasto", string.IsNullOrWhiteSpace(city) ? (object)DBNull.Value : city);
        cmd.Parameters.AddWithValue("@Waluta", string.IsNullOrWhiteSpace(currency) ? "PLN" : currency);

        var newId = await cmd.ExecuteInsertAndGetIdAsync(cancellationToken);
        return (int)newId;
    }
}
