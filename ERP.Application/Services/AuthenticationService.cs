using ERP.Application.DTOs;
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
    private readonly IUserCompanyRepository _userCompanyRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRoleRepository _roleRepository;

    public AuthenticationService(
        IUserRepository userRepository,
        IUserLoginRepository userLoginRepository,
        IUserCompanyRepository userCompanyRepository,
        ICompanyRepository companyRepository,
        IRoleRepository roleRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userLoginRepository = userLoginRepository ?? throw new ArgumentNullException(nameof(userLoginRepository));
        _userCompanyRepository = userCompanyRepository ?? throw new ArgumentNullException(nameof(userCompanyRepository));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
    }

    /// <summary>
    /// Autentykacja użytkownika na podstawie loginu i hasła
    /// </summary>
    public async Task<UserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        // Pobieramy dane logowania z tabeli operator_login
        var userLogin = await _userLoginRepository.GetByLoginAsync(username, cancellationToken);
        if (userLogin == null)
            return null;

        // Weryfikujemy hasło
        if (!await VerifyPasswordAsync(password, userLogin.PasswordHash))
            return null;

        // Pobieramy dane użytkownika z tabeli operator
        var user = await _userRepository.GetByIdAsync(userLogin.UserId, cancellationToken);
        if (user == null)
            return null;

        return MapToDto(user);
    }

    /// <summary>
    /// Pobiera listę firm dostępnych dla użytkownika wraz z rolami
    /// </summary>
    public async Task<IEnumerable<CompanyDto>> GetUserCompaniesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userCompanies = await _userCompanyRepository.GetByUserIdAsync(userId, cancellationToken);
        var companies = new List<CompanyDto>();

        foreach (var userCompany in userCompanies)
        {
            var company = await _companyRepository.GetByIdAsync(userCompany.CompanyId, cancellationToken);
            if (company != null)
            {
                var isDefault = false;
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user != null)
                {
                    isDefault = company.Id == user.DefaultCompanyId;
                }

                companies.Add(new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Header1 = company.Header1,
                    Header2 = company.Header2,
                    Street = company.Street,
                    PostalCode = company.PostalCode,
                    City = company.City,
                    Country = company.Country,
                    Nip = company.Nip,
                    Regon = company.Regon,
                    Krs = company.Krs,
                    Phone1 = company.Phone1,
                    Email = company.Email,
                    RoleId = userCompany.RoleId,
                    IsDefault = isDefault
                });
            }
        }

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
