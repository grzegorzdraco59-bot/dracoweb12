using ERP.Application.DTOs;
using ERP.Infrastructure.Data;
using ERP.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Proste repozytorium do ładowania operacji magazynowych z tabeli magazyn
/// </summary>
public class WarehouseRepository
{
    private readonly DatabaseContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WarehouseRepository(DatabaseContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private int GetCurrentCompanyId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            throw new InvalidOperationException("Brak kontekstu HTTP. Metoda musi być wywołana w kontekście requestu HTTP.");

        var companyId = httpContext.User.GetCompanyId();
        if (!companyId.HasValue)
            throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
        return companyId.Value;
    }

    public async Task<IEnumerable<WarehouseItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await GetByCompanyIdAsync(GetCurrentCompanyId(), cancellationToken);
    }

    public async Task<IEnumerable<WarehouseItemDto>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var items = new List<WarehouseItemDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT id_magazyn, id_firmy, data_operacji, nr_dok_zak_sprz, nazwa_dostawcy_odbiorcy, " +
            "id_towaru, nazwa_towaru, jednostki_magazynu, ilosc_pz, ilosc_wz, cena_sprzedazy " +
            "FROM magazyn WHERE id_firmy = @CompanyId ORDER BY data_operacji DESC, id_magazyn DESC",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var operationDate = reader.IsDBNull(reader.GetOrdinal("data_operacji")) ? null : 
                ConvertClarionDate(reader.GetInt32(reader.GetOrdinal("data_operacji")));
            
            items.Add(new WarehouseItemDto
            {
                Id = reader.IsDBNull(reader.GetOrdinal("id_magazyn")) ? 0 : reader.GetInt32(reader.GetOrdinal("id_magazyn")),
                CompanyId = reader.IsDBNull(reader.GetOrdinal("id_firmy")) ? null : reader.GetInt32(reader.GetOrdinal("id_firmy")),
                OperationDate = operationDate,
                DocumentNumber = reader.IsDBNull(reader.GetOrdinal("nr_dok_zak_sprz")) ? null : reader.GetString(reader.GetOrdinal("nr_dok_zak_sprz")),
                SupplierCustomerName = reader.IsDBNull(reader.GetOrdinal("nazwa_dostawcy_odbiorcy")) ? null : reader.GetString(reader.GetOrdinal("nazwa_dostawcy_odbiorcy")),
                ProductId = reader.IsDBNull(reader.GetOrdinal("id_towaru")) ? null : reader.GetInt32(reader.GetOrdinal("id_towaru")),
                ProductName = reader.IsDBNull(reader.GetOrdinal("nazwa_towaru")) ? null : reader.GetString(reader.GetOrdinal("nazwa_towaru")),
                Unit = reader.IsDBNull(reader.GetOrdinal("jednostki_magazynu")) ? null : reader.GetString(reader.GetOrdinal("jednostki_magazynu")),
                QuantityIn = reader.IsDBNull(reader.GetOrdinal("ilosc_pz")) ? null : reader.GetDecimal(reader.GetOrdinal("ilosc_pz")),
                QuantityOut = reader.IsDBNull(reader.GetOrdinal("ilosc_wz")) ? null : reader.GetDecimal(reader.GetOrdinal("ilosc_wz")),
                Price = reader.IsDBNull(reader.GetOrdinal("cena_sprzedazy")) ? null : reader.GetDecimal(reader.GetOrdinal("cena_sprzedazy"))
            });
        }

        return items;
    }

    private static string? ConvertClarionDate(int? clarionDate)
    {
        if (!clarionDate.HasValue)
            return null;
        
        try
        {
            var baseDate = new DateTime(1800, 12, 28);
            var dateTime = baseDate.AddDays(clarionDate.Value);
            return dateTime.ToString("dd/MM/yyyy");
        }
        catch
        {
            return clarionDate.Value.ToString();
        }
    }
}
