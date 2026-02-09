using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Lookup kontrahentów (kontrahenci_v) – tylko SELECT.
/// </summary>
public class KontrahenciQueryRepository : IKontrahenciQueryRepository
{
    private readonly DatabaseContext _context;

    public KontrahenciQueryRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<IEnumerable<KontrahentLookupDto>> GetAllForCompanyAsync(int companyId, CancellationToken cancellationToken = default)
        => SearchAsync(companyId, null, cancellationToken);

    public async Task<IEnumerable<KontrahentLookupDto>> SearchAsync(int companyId, string? queryText, CancellationToken cancellationToken = default)
    {
        var list = new List<KontrahentLookupDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT * " +
            "FROM kontrahenci_v " +
            "WHERE company_id = @CompanyId " +
            "AND (@q IS NULL OR nazwa LIKE CONCAT('%',@q,'%') OR email LIKE CONCAT('%',@q,'%')) " +
            "ORDER BY nazwa",
            connection);
        cmd.Parameters.AddWithValue("@CompanyId", companyId);
        cmd.Parameters.AddWithValue("@q", string.IsNullOrWhiteSpace(queryText) ? (object)DBNull.Value : queryText);

        await using var reader = await cmd.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        var columns = GetColumnLookup(reader);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new KontrahentLookupDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                KontrahentId = reader.IsDBNull(reader.GetOrdinal("id")) ? null : reader.GetInt32(reader.GetOrdinal("id")),
                CompanyId = GetNullableInt(reader, columns, "company_id"),
                Typ = GetNullableString(reader, columns, "typ"),
                Nazwa = GetNullableString(reader, columns, "nazwa"),
                UlicaINr = GetNullableString(reader, columns, "ulica_i_nr", "ulica", "adres", "ulica_nr"),
                KodPocztowy = GetNullableString(reader, columns, "kod_pocztowy", "kod_poczt"),
                Miasto = GetNullableString(reader, columns, "miasto"),
                Panstwo = GetNullableString(reader, columns, "panstwo"),
                Nip = GetNullableString(reader, columns, "nip"),
                Email = GetNullableString(reader, columns, "email"),
                Telefon = GetNullableString(reader, columns, "telefon"),
                Waluta = GetNullableString(reader, columns, "waluta")
            });
        }
        return list;
    }

    private static HashSet<string> GetColumnLookup(MySqlDataReader reader)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in reader.GetColumnSchema())
            columns.Add(column.ColumnName ?? string.Empty);
        return columns;
    }

    private static string? GetNullableString(MySqlDataReader reader, HashSet<string> columns, params string[] names)
    {
        foreach (var name in names)
        {
            if (!columns.Contains(name))
                continue;
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
        return null;
    }

    private static int? GetNullableInt(MySqlDataReader reader, HashSet<string> columns, params string[] names)
    {
        foreach (var name in names)
        {
            if (!columns.Contains(name))
                continue;
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
        return null;
    }
}
