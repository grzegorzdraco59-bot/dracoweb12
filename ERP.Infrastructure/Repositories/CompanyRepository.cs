using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Company (firmy) używająca MySqlConnector
/// </summary>
public class CompanyRepository : ICompanyRepository, ICompanyQueryRepository
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
            "SELECT id_firmy AS id, NAZWA, NAGLOWEK1, NAGLOWEK2, ULICA_NR, KOD_POCZTOWY, MIASTO, PANSTWO, " +
            "NIP, REGON, krs, TEL1, MAIL, Numeracja_FV_r_m, Numeracja_FPF_r_m, " +
            "smtp_host, smtp_port, smtp_user, smtp_pass, smtp_ssl, smtp_from_email, smtp_from_name " +
            "FROM firmy WHERE id_firmy = @Id",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "SELECT id_firmy AS id, NAZWA, NAGLOWEK1, NAGLOWEK2, ULICA_NR, KOD_POCZTOWY, MIASTO, PANSTWO, " +
            "NIP, REGON, krs, TEL1, MAIL, Numeracja_FV_r_m, Numeracja_FPF_r_m, " +
            "smtp_host, smtp_port, smtp_user, smtp_pass, smtp_ssl, smtp_from_email, smtp_from_name " +
            "FROM firmy ORDER BY NAZWA",
            connection);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
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
            "SELECT DISTINCT f.id_firmy AS id, f.NAZWA, f.NAGLOWEK1, f.NAGLOWEK2, f.ULICA_NR, f.KOD_POCZTOWY, " +
            "f.MIASTO, f.PANSTWO, f.NIP, f.REGON, f.krs, f.TEL1, f.MAIL, f.Numeracja_FV_r_m, f.Numeracja_FPF_r_m, " +
            "f.smtp_host, f.smtp_port, f.smtp_user, f.smtp_pass, f.smtp_ssl, f.smtp_from_email, f.smtp_from_name " +
            "FROM firmy f " +
            "INNER JOIN `operatorfirma` of ON f.id_firmy = of.id_firmy " +
            "WHERE of.id_operatora = @UserId " +
            "ORDER BY f.NAZWA",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            companies.Add(MapToCompany(reader));
        }

        return companies;
    }

    /// <summary>
    /// Walidacja: SELECT COUNT(*) FROM operatorfirma WHERE id_operatora = @UserId.
    /// Bez LIMIT 1, bez IsActive, bez warunków po operatorfirma.id.
    /// </summary>
    public async Task<int> GetCompanyCountByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(*) FROM `operatorfirma` WHERE id_operatora = @UserId",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Pobiera firmy użytkownika z rolami (ta sama logika: id_operatora = @UserId).
    /// WHERE operatorfirma.id_operatora = @UserId. Bez LIMIT 1, bez IsActive, bez warunków po operatorfirma.id.
    /// </summary>
    public async Task<IEnumerable<CompanyDto>> GetCompanyDtosByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var companies = new List<CompanyDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT f.id_firmy AS id, f.NAZWA, f.NAGLOWEK1, f.NAGLOWEK2, f.ULICA_NR, f.KOD_POCZTOWY, " +
            "f.MIASTO, f.PANSTWO, f.NIP, f.REGON, f.krs, f.TEL1, f.MAIL, f.Numeracja_FV_r_m, f.Numeracja_FPF_r_m, " +
            "f.smtp_host, f.smtp_port, f.smtp_user, f.smtp_pass, f.smtp_ssl, f.smtp_from_email, f.smtp_from_name, " +
            "of.rola AS RoleId, o.id_firmy AS DefaultCompanyId " +
            "FROM firmy f " +
            "INNER JOIN `operatorfirma` of ON f.id_firmy = of.id_firmy " +
            "LEFT JOIN `operator` o ON o.id = of.id_operatora " +
            "WHERE of.id_operatora = @UserId " +
            "ORDER BY f.NAZWA",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            companies.Add(MapToCompanyDto(reader));
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
        var newId = await command.ExecuteInsertAndGetIdAsync(cancellationToken);
        return (int)newId;
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE firmy SET NAZWA = @Name, NAGLOWEK1 = @Header1, NAGLOWEK2 = @Header2, " +
            "ULICA_NR = @Street, KOD_POCZTOWY = @PostalCode, MIASTO = @City, PANSTWO = @Country, " +
            "NIP = @Nip, REGON = @Regon, krs = @Krs, TEL1 = @Phone1, MAIL = @Email, " +
            "Numeracja_FV_r_m = @InvoiceNumberingPerMonth, Numeracja_FPF_r_m = @ProformaInvoiceNumberingPerMonth " +
            "WHERE id_firmy = @Id",
            connection);

        command.Parameters.AddWithValue("@Id", company.Id);
        AddCompanyParameters(command, company);

        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand("SELECT COUNT(1) FROM firmy WHERE id_firmy = @Id", connection);
        command.Parameters.AddWithValue("@Id", id);
        var result = await command.ExecuteScalarWithDiagnosticsAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static Company MapToCompany(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id"));
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

        var smtpHost = GetNullableString(reader, "smtp_host");
        var smtpPort = GetNullableInt(reader, "smtp_port");
        var smtpUser = GetNullableString(reader, "smtp_user");
        var smtpPass = GetNullableString(reader, "smtp_pass");
        var smtpSsl = GetNullableBool(reader, "smtp_ssl");
        var smtpFromEmail = GetNullableString(reader, "smtp_from_email");
        var smtpFromName = GetNullableString(reader, "smtp_from_name");
        company.SetSmtpSettings(smtpHost, smtpPort, smtpUser, smtpPass, smtpSsl, smtpFromEmail, smtpFromName);

        return company;
    }

    private static CompanyDto MapToCompanyDto(MySqlDataReader reader)
    {
        var id = reader.GetInt32(reader.GetOrdinal("id"));
        var name = reader.IsDBNull(reader.GetOrdinal("NAZWA")) ? string.Empty : reader.GetString(reader.GetOrdinal("NAZWA"));
        var defaultCompanyId = GetNullableInt(reader, "DefaultCompanyId");
        var roleId = GetNullableInt(reader, "RoleId");

        return new CompanyDto
        {
            Id = id,
            Name = name,
            Header1 = GetNullableString(reader, "NAGLOWEK1"),
            Header2 = GetNullableString(reader, "NAGLOWEK2"),
            Street = GetNullableString(reader, "ULICA_NR"),
            PostalCode = GetNullableString(reader, "KOD_POCZTOWY"),
            City = GetNullableString(reader, "MIASTO"),
            Country = GetNullableString(reader, "PANSTWO"),
            Nip = GetNullableString(reader, "NIP"),
            Regon = GetNullableString(reader, "REGON"),
            Krs = GetNullableString(reader, "krs"),
            Phone1 = GetNullableString(reader, "TEL1"),
            Email = GetNullableString(reader, "MAIL"),
            RoleId = roleId,
            IsDefault = defaultCompanyId.HasValue && defaultCompanyId.Value == id,
            SmtpHost = GetNullableString(reader, "smtp_host"),
            SmtpPort = GetNullableInt(reader, "smtp_port"),
            SmtpUser = GetNullableString(reader, "smtp_user"),
            SmtpPass = GetNullableString(reader, "smtp_pass"),
            SmtpSsl = GetNullableBool(reader, "smtp_ssl"),
            SmtpFromEmail = GetNullableString(reader, "smtp_from_email"),
            SmtpFromName = GetNullableString(reader, "smtp_from_name")
        };
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static int? GetNullableInt(MySqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static bool? GetNullableBool(MySqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
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
