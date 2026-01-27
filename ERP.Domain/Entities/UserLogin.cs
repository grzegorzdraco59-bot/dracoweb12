namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca dane logowania użytkownika
/// Mapuje do tabeli: operator_login
/// </summary>
public class UserLogin : BaseEntity
{
    public int UserId { get; private set; }
    public string Login { get; private set; }
    public string PasswordHash { get; private set; }

    // Konstruktor prywatny dla EF Core
    private UserLogin()
    {
        Login = string.Empty;
        PasswordHash = string.Empty;
    }

    // Główny konstruktor
    public UserLogin(int userId, string login, string passwordHash)
    {
        if (userId <= 0)
            throw new ArgumentException("Id użytkownika musi być większe od zera.", nameof(userId));
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Login nie może być pusty.", nameof(login));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Hash hasła nie może być pusty.", nameof(passwordHash));

        UserId = userId;
        Login = login;
        PasswordHash = passwordHash;
    }

    public void UpdateLogin(string newLogin)
    {
        if (string.IsNullOrWhiteSpace(newLogin))
            throw new ArgumentException("Login nie może być pusty.", nameof(newLogin));

        Login = newLogin;
        UpdateTimestamp();
    }

    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Hash hasła nie może być pusty.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdateTimestamp();
    }
}
