using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Proste repozytorium do ładowania towarów z tabeli towary
/// </summary>
public class ProductRepository
{
    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;

    public ProductRepository(DatabaseContext context, IUserContext userContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    private int GetCurrentCompanyId()
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
        return companyId.Value;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = new List<ProductDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_towar, id_firmy, grupa, Nazwa_PL, Nazwa_ENG, Cena_PLN, jednostki_sprzedazy, status_towaru, " +
            "id_dostawcy, dostawca " +
            "FROM towary WHERE id_firmy = @CompanyId ORDER BY Nazwa_PL",
            connection);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new ProductDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("ID_towar")),
                CompanyId = reader.IsDBNull(reader.GetOrdinal("id_firmy")) ? null : reader.GetInt32(reader.GetOrdinal("id_firmy")),
                Group = reader.IsDBNull(reader.GetOrdinal("grupa")) ? null : reader.GetString(reader.GetOrdinal("grupa")),
                NamePl = reader.IsDBNull(reader.GetOrdinal("Nazwa_PL")) ? null : reader.GetString(reader.GetOrdinal("Nazwa_PL")),
                NameEng = reader.IsDBNull(reader.GetOrdinal("Nazwa_ENG")) ? null : reader.GetString(reader.GetOrdinal("Nazwa_ENG")),
                PricePln = reader.IsDBNull(reader.GetOrdinal("Cena_PLN")) ? null : reader.GetDecimal(reader.GetOrdinal("Cena_PLN")),
                Unit = reader.IsDBNull(reader.GetOrdinal("jednostki_sprzedazy")) ? null : reader.GetString(reader.GetOrdinal("jednostki_sprzedazy")),
                Status = reader.IsDBNull(reader.GetOrdinal("status_towaru")) ? null : reader.GetString(reader.GetOrdinal("status_towaru")),
                SupplierId = reader.IsDBNull(reader.GetOrdinal("id_dostawcy")) ? null : reader.GetInt32(reader.GetOrdinal("id_dostawcy")),
                SupplierName = reader.IsDBNull(reader.GetOrdinal("dostawca")) ? null : reader.GetString(reader.GetOrdinal("dostawca"))
            });
        }

        return products;
    }
}
