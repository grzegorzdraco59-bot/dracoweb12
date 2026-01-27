using System.Security.Claims;

namespace ERP.Shared.Extensions;

/// <summary>
/// Extensions dla ClaimsPrincipal do pobierania informacji o użytkowniku i firmie
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string CompanyIdClaimType = "CompanyId";
    private const string RoleIdClaimType = "RoleId";

    /// <summary>
    /// Pobiera ID użytkownika z Claims
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? 
                         principal.FindFirst("UserId");
        
        if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value) || !int.TryParse(userIdClaim.Value, out int userId))
            return null;
            
        return userId;
    }

    /// <summary>
    /// Pobiera ID firmy z Claims
    /// </summary>
    public static int? GetCompanyId(this ClaimsPrincipal principal)
    {
        var companyIdClaim = principal.FindFirst(CompanyIdClaimType);
        
        if (companyIdClaim == null || string.IsNullOrEmpty(companyIdClaim.Value) || !int.TryParse(companyIdClaim.Value, out int companyId))
            return null;
            
        return companyId;
    }

    /// <summary>
    /// Pobiera ID roli z Claims
    /// </summary>
    public static int? GetRoleId(this ClaimsPrincipal principal)
    {
        var roleIdClaim = principal.FindFirst(RoleIdClaimType);
        
        if (roleIdClaim == null || string.IsNullOrEmpty(roleIdClaim.Value) || !int.TryParse(roleIdClaim.Value, out int roleId))
            return null;
            
        return roleId;
    }

    /// <summary>
    /// Sprawdza czy użytkownik ma wybraną firmę
    /// </summary>
    public static bool HasCompanySelected(this ClaimsPrincipal principal)
    {
        return GetCompanyId(principal).HasValue;
    }

    /// <summary>
    /// Pobiera nazwę użytkownika z Claims
    /// </summary>
    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        var nameClaim = principal.FindFirst(ClaimTypes.Name);
        return nameClaim?.Value;
    }
}
