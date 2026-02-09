using ERP.Application.DTOs;

namespace ERP.Application.Services;

/// <summary>
/// Interfejs serwisu autentykacji
/// </summary>
public interface IAuthenticationService
{
    Task<UserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    /// <summary>Walidacja: SELECT COUNT(*) FROM operatorfirma WHERE id_operatora = @UserId. Zwraca true gdy COUNT > 0.</summary>
    Task<bool> HasCompaniesForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompanyDto>> GetUserCompaniesAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordAsync(string password, string passwordHash);
    string HashPassword(string password);
}
