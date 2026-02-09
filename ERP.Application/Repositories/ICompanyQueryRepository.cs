using ERP.Application.DTOs;

namespace ERP.Application.Repositories;

/// <summary>
/// Interfejs do pobierania firm z rolami użytkownika (dla logowania).
/// Używa jednego zapytania: operatorfirma.id_operatora = @UserId.
/// </summary>
public interface ICompanyQueryRepository
{
    /// <summary>
    /// Walidacja: liczba rekordów w operatorfirma dla użytkownika.
    /// SELECT COUNT(*) FROM operatorfirma WHERE id_operatora = @UserId.
    /// </summary>
    Task<int> GetCompanyCountByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera firmy przypisane użytkownikowi wraz z rolami.
    /// WHERE operatorfirma.id_operatora = @UserId.
    /// </summary>
    Task<IEnumerable<CompanyDto>> GetCompanyDtosByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
