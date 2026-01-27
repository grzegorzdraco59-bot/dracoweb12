namespace ERP.UI.WPF.Services;

/// <summary>
/// Interfejs serwisu przechowującego kontekst zalogowanego użytkownika w aplikacji WPF
/// </summary>
public interface IUserContext
{
    int? UserId { get; }
    int? CompanyId { get; }
    int? RoleId { get; }
    string? Username { get; }
    bool IsLoggedIn { get; }

    void SetSession(int userId, int companyId, int? roleId, string username);
    void ClearSession();
    void ChangeCompany(int companyId, int? roleId = null);
}

/// <summary>
/// Implementacja serwisu przechowującego kontekst zalogowanego użytkownika w aplikacji WPF
/// Scoped service - jeden na całą sesję aplikacji
/// </summary>
public class UserContext : IUserContext
{
    private int? _userId;
    private int? _companyId;
    private int? _roleId;
    private string? _username;

    public int? UserId => _userId;
    public int? CompanyId => _companyId;
    public int? RoleId => _roleId;
    public string? Username => _username;
    public bool IsLoggedIn => _userId.HasValue && _companyId.HasValue;

    public void SetSession(int userId, int companyId, int? roleId, string username)
    {
        _userId = userId;
        _companyId = companyId;
        _roleId = roleId;
        _username = username;
    }

    public void ClearSession()
    {
        _userId = null;
        _companyId = null;
        _roleId = null;
        _username = null;
    }

    public void ChangeCompany(int companyId, int? roleId = null)
    {
        if (!_userId.HasValue)
            throw new InvalidOperationException("Użytkownik nie jest zalogowany.");

        _companyId = companyId;
        _roleId = roleId;
    }
}
