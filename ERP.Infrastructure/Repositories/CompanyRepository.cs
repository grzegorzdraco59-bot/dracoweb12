using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Company (firmy) używająca MySqlConnector
/// </summary>
public class CompanyRepository : ICompanyRepository
{
    private readonly DatabaseContext _context;

    public CompanyRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_FIRMY, NAZWA, NAGLOWEK1, NAGLOWEK2, ULICA_NR, KOD_POCZTOWY, MIASTO, PANSTWO, " +
            "NIP, REGON, krs, TEL1, MAIL, Numeracja_FV_r_m, Numeracja_FPF_r_m " +
            "FROM firmy WHERE ID_FIRMY = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToCompany(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var companies = new List<Company>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_FIRMY, NAZWA, NAGLOWEK1, NAGLOWEK2, ULICA_NR, KOD_POCZTOWY, MIASTO, PANSTWO, " +
            "NIP, REGON, krs, TEL1, MAIL, Numeracja_FV_r_m, Numeracja_FPF_r_m " +
            "FROM firmy ORDER BY NAZWA",
            connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            companies.Add(MapToCompany(reader));
        }

        return companies;
    }

    public async Task<IEnumerable<Company>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var companies = new List<Company>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT DISTINCT f.ID_FIRMY, f.NAZWA, f.NAGLOWEK1, f.NAGLOWEK2, f.ULICA_NR, f.KOD_POCZTOWY, " +
            "f.MIASTO, f.PANSTWO, f.NIP, f.REGON, f.krs, f.TEL1, f.MAIL, f.Numeracja_FV_r_m, f.Numeracja_FPF_r_m " +
            "FROM firmy f " +
            "INNER JOIN operatorfirma of ON f.ID_FIRMY = of.id_firmy " +
            "WHERE of.id_operatora = @UserId " +
            "ORDER BY f.NAZWA",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            companies.Add(MapToCompany(reader));
        }

        return companies;
    }

    public async Task<int> AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO firmy (NAZWA, NAGLOWEK1, NAGLOWEK2, ULICA_NR, KOD_POCZTOWY, MIASTO, PANSTWO, " +
            "NIP, REGON, krs, TEL1, MAIL, Numeracja_FV_r_m, Numeracja_FPF_r_m) " +
            "VALUES (@Name, @Header1, @Header2, @Street, @PostalCode, @City, @Country, " +
            "@Nip, @Regon, @Krs, @Phone1, @Email, @InvoiceNumberingPerMonth, @ProformaInvoiceNumberingPerMonth); " +
            "SELECT LAST_INSERT_ID();",
            connection);

        AddCompanyParameters(command, company);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE firmy SET NAZWA = @Name, NAGLOWEK1 = @Header1, NAGLOWEK2 = @Header2, " +
            "ULICA_NR = @Street, KOD_POCZTOWY = @PostalCode, MIASTO = @City, PANSTWO = @Country, " +
            "NIP = @Nip, REGON = @Regon, krs = @Krs, TEL1 = @Phone1, MAIL = @Email, " +
            "Numeracja_FV_r_m = @InvoiceNumberingPerMonth, Numeracja_FPF_r_m = @ProformaInvoiceNumberingPerMonth " +
            "WHERE ID_FIRMY = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", company.Id);
        AddCompanyParameters(command, company);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("SELECT COUNT(1) FROM firmy WHERE ID_FIRMY = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static Company MapToCompany(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("ID_FIRMY"));
        var name = reader.IsDBNull(reader.GetOrdinal("NAZWA")) ? string.Empty : reader.GetString(reader.GetOrdinal("NAZWA"));
        
        var company = new Company(name);
        
        // Ustawiamy Id używając refleksji
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        idProperty.SetValue(company, id);

        var header1 = reader.IsDBNull(reader.GetOrdinal("NAGLOWEK1")) ? null : reader.GetString(reader.GetOrdinal("NAGLOWEK1"));
        var header2 = reader.IsDBNull(reader.GetOrdinal("NAGLOWEK2")) ? null : reader.GetString(reader.GetOrdinal("NAGLOWEK2"));
        company.UpdateHeaders(header1, header2);

        var street = reader.IsDBNull(reader.GetOrdinal("ULICA_NR")) ? null : reader.GetString(reader.GetOrdinal("ULICA_NR"));
        var postalCode = reader.IsDBNull(reader.GetOrdinal("KOD_POCZTOWY")) ? null : reader.GetString(reader.GetOrdinal("KOD_POCZTOWY"));
        var city = reader.IsDBNull(reader.GetOrdinal("MIASTO")) ? null : reader.GetString(reader.GetOrdinal("MIASTO"));
        var country = reader.IsDBNull(reader.GetOrdinal("PANSTWO")) ? null : reader.GetString(reader.GetOrdinal("PANSTWO"));
        company.UpdateAddress(street, postalCode, city, country);

        var nip = reader.IsDBNull(reader.GetOrdinal("NIP")) ? null : reader.GetString(reader.GetOrdinal("NIP"));
        var regon = reader.IsDBNull(reader.GetOrdinal("REGON")) ? null : reader.GetString(reader.GetOrdinal("REGON"));
        var krs = reader.IsDBNull(reader.GetOrdinal("krs")) ? null : reader.GetString(reader.GetOrdinal("krs"));
        var phone1 = reader.IsDBNull(reader.GetOrdinal("TEL1")) ? null : reader.GetString(reader.GetOrdinal("TEL1"));
        var email = reader.IsDBNull(reader.GetOrdinal("MAIL")) ? null : reader.GetString(reader.GetOrdinal("MAIL"));
        company.UpdateCompanyData(nip, regon, krs, phone1, email);

        var invoiceNumberingPerMonth = !reader.IsDBNull(reader.GetOrdinal("Numeracja_FV_r_m")) && reader.GetBoolean(reader.GetOrdinal("Numeracja_FV_r_m"));
        var proformaInvoiceNumberingPerMonth = !reader.IsDBNull(reader.GetOrdinal("Numeracja_FPF_r_m")) && reader.GetBoolean(reader.GetOrdinal("Numeracja_FPF_r_m"));
        company.SetInvoiceNumberingPerMonth(invoiceNumberingPerMonth);
        company.SetProformaInvoiceNumberingPerMonth(proformaInvoiceNumberingPerMonth);

        return company;
    }

    private static void AddCompanyParameters(MySqlCommand command, Company company)
    {
        command.Parameters.AddWithValue("@Name", company.Name ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Header1", company.Header1 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Header2", company.Header2 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Street", company.Street ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PostalCode", company.PostalCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@City", company.City ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Country", company.Country ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Nip", company.Nip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Regon", company.Regon ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Krs", company.Krs ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Phone1", company.Phone1 ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Email", company.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@InvoiceNumberingPerMonth", company.InvoiceNumberingPerMonth);
        command.Parameters.AddWithValue("@ProformaInvoiceNumberingPerMonth", company.ProformaInvoiceNumberingPerMonth);
    }
}
