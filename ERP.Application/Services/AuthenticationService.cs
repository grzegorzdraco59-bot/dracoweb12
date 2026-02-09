using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using BCrypt.Net;

namespace ERP.Application.Services;

/// <summary>
/// Implementacja serwisu autentykacji
/// Używa tabeli operator_login do weryfikacji loginu i hasła
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserLoginRepository _userLoginRepository;
    private readonly ICompanyQueryRepository _companyQueryRepository;

    public AuthenticationService(
        IUserRepository userRepository,
        IUserLoginRepository userLoginRepository,
        ICompanyQueryRepository companyQueryRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _companyQueryRepository = companyQueryRepository ?? throw new ArgumentNullException(nameof(companyQueryRepository));
    }

    /// <summary>
    /// Autentykacja użytkownika na podstawie loginu i hasła.
    /// Tryb testowy: gdy login jest liczbą (np. "1"), logowanie po ID operatora – bez weryfikacji hasła (tylko do weryfikacji).
    /// </summary>
    public async Task<UserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        // Tryb testowy: logowanie po ID (gdy login to liczba) – bez operator_login, bez hasła
        if (int.TryParse(username.Trim(), out var operatorId) && operatorId > 0)
        {
            var user = await _userRepository.GetByIdAsync(operatorId, cancellationToken);
            if (user != null)
                return MapToDto(user);
            // Jeśli nie znaleziono – fallback do normalnego logowania
        }

        if (string.IsNullOrWhiteSpace(password))
            return null;

        // Normalne logowanie: operator_login (login + hasło)
        var userLogin = await _userLoginRepository.GetByLoginAsync(username, cancellationToken);
        if (userLogin == null)
            return null;

        if (!await VerifyPasswordAsync(password, userLogin.PasswordHash))
            return null;

        var userByLogin = await _userRepository.GetByIdAsync(userLogin.UserId, cancellationToken);
        if (userByLogin == null)
            return null;

        return MapToDto(userByLogin);
    }

    /// <summary>
    /// Walidacja: SELECT COUNT(*) FROM operatorfirma WHERE id_operatora = @UserId.
    /// Zwraca true gdy COUNT > 0 (użytkownik ma przypisane firmy).
    /// </summary>
    public async Task<bool> HasCompaniesForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var count = await _companyQueryRepository.GetCompanyCountByUserIdAsync(userId, cancellationToken);
        return count > 0;
    }

    /// <summary>
    /// Pobiera listę firm dostępnych dla użytkownika wraz z rolami.
    /// Jedno zapytanie: WHERE operatorfirma.id_operatora = @UserId.
    /// Brak firm zgłaszany tylko gdy zapytanie zwraca 0 wierszy.
    /// </summary>
    public async Task<IEnumerable<CompanyDto>> GetUserCompaniesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var companies = await _companyQueryRepository.GetCompanyDtosByUserIdAsync(userId, cancellationToken);
        return companies.OrderByDescending(c => c.IsDefault).ThenBy(c => c.Name);
    }

    /// <summary>
    /// Weryfikacja hasła używając BCrypt
    /// </summary>
    public async Task<bool> VerifyPasswordAsync(string password, string passwordHash)
    {
        // Sprawdzamy czy hash jest w starym formacie SHA256 (Base64) czy nowym BCrypt
        // BCrypt hash zawsze zaczyna się od $2a$, $2b$, $2x$ lub $2y$
        if (passwordHash.StartsWith("$2"))
        {
            // Nowy format BCrypt
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        else
        {
            // Stary format SHA256 - dla kompatybilności wstecznej
            // W przyszłości można usunąć tę część po migracji wszystkich haseł
            var sha256Hash = HashPasswordSHA256(password);
            return sha256Hash == passwordHash;
        }
    }

    /// <summary>
    /// Hashowanie hasła używając BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Hashowanie hasła używając SHA256 (tylko dla kompatybilności wstecznej)
    /// </summary>
    private static string HashPasswordSHA256(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            DefaultCompanyId = user.DefaultCompanyId,
            FullName = user.FullName,
            Permissions = user.Permissions
        };
    }
}
