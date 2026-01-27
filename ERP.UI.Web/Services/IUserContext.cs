namespace ERP.UI.Web.Services;

/// <summary>
/// Interfejs serwisu przechowującego kontekst zalogowanego użytkownika w aplikacji webowej
/// Oparty o Claims z HttpContext.User
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// ID zalogowanego użytkownika (z Claims)
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// ID aktywnie wybranej firmy (z Claims)
    /// </summary>
    int? CompanyId { get; }

    /// <summary>
    /// ID roli użytkownika w aktywnej firmie (z Claims)
    /// </summary>
    int? RoleId { get; }

    /// <summary>
    /// Nazwa użytkownika (z Claims)
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Lista ról użytkownika (ClaimTypes.Role). Nieużywane w obecnym modelu – RoleId jest źródłem prawdy.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Sprawdza czy użytkownik jest zalogowany
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Sprawdza czy użytkownik ma wybraną firmę
    /// </summary>
    bool HasCompanySelected { get; }
}
