using System.Security.Claims;
using ERP.Shared.Extensions;
using Microsoft.AspNetCore.Http;

namespace ERP.UI.Web.Services;

/// <summary>
/// Implementacja IUserContext oparta o IHttpContextAccessor i Claims z HttpContext.User
/// Scoped service - jeden na request
/// </summary>
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId => User?.GetUserId();

    public int? CompanyId => User?.GetCompanyId();

    public int? RoleId => User?.GetRoleId();

    public string? UserName => User?.GetUserName();

    public IEnumerable<string> Roles
    {
        get
        {
            if (User == null)
                return Enumerable.Empty<string>();

            return User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasCompanySelected => CompanyId.HasValue;
}
