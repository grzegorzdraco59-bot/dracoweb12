using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Application.Services;
using ERP.Application.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ERP.Shared.Extensions;
using ERP.UI.Web.Services;
using AppAuthService = ERP.Application.Services.IAuthenticationService;
using IUserContext = ERP.UI.Web.Services.IUserContext;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler do wyboru firmy przez zalogowanego użytkownika
/// </summary>
[Authorize] // Wymaga autentykacji, ale nie policy (użytkownik dopiero wybiera firmę)
public class CompanyController : Controller
{
    private readonly AppAuthService _authenticationService;
    private readonly IUserContext _userContext;

    public CompanyController(AppAuthService authenticationService, IUserContext userContext)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Wyświetla listę firm przypisanych do zalogowanego użytkownika jako przyciski
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Select(int? testUserId = null, CancellationToken cancellationToken = default)
    {
        // Model mentalny: Autoryzacja = Identity / Claims
        // Pobieramy UserId z IUserContext (główny sposób)
        int? userId = _userContext.UserId;
        
        // Fallback: Tymczasowo pozwól na testowanie z parametrem query string
        if (!userId.HasValue && testUserId.HasValue)
        {
            userId = testUserId.Value;
        }
        
        // Jeśli nadal nie ma UserId - użytkownik nie jest zalogowany, przekieruj do logowania
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Pobieramy listę firm użytkownika
        var companies = await _authenticationService.GetUserCompaniesAsync(userId.Value, cancellationToken);
        var companiesList = companies.ToList();

        if (!companiesList.Any())
        {
            ViewBag.ErrorMessage = "Nie masz przypisanych żadnych firm. Skontaktuj się z administratorem.";
            return View(new List<CompanyDto>());
        }

        return View(companiesList);
    }

    /// <summary>
    /// Zapisuje wybraną firmę w Claims i przekierowuje do głównej strony
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int companyId, int? roleId = null)
    {
        if (companyId <= 0)
        {
            ModelState.AddModelError("", "Nieprawidłowe ID firmy.");
            return RedirectToAction(nameof(Select));
        }

        // Pobieramy UserId z IUserContext
        var userId = _userContext.UserId;
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Pobieramy listę firm użytkownika do walidacji
        var companies = await _authenticationService.GetUserCompaniesAsync(userId.Value, CancellationToken.None);
        var userCompany = companies.FirstOrDefault(c => c.Id == companyId);
        
        if (userCompany == null)
        {
            ModelState.AddModelError("", "Nie masz dostępu do wybranej firmy.");
            return RedirectToAction(nameof(Select));
        }

        // Aktualizujemy Claims - dodajemy CompanyId i RoleId
        var claims = User.Claims.ToList();
        
        // Usuwamy stare Claims CompanyId i RoleId jeśli istnieją
        claims.RemoveAll(c => c.Type == "CompanyId" || c.Type == "RoleId");
        
        // Dodajemy nowe Claims
        claims.Add(new Claim("CompanyId", companyId.ToString()));
        if (roleId.HasValue)
        {
            claims.Add(new Claim("RoleId", roleId.Value.ToString()));
        }
        else if (userCompany.RoleId.HasValue)
        {
            claims.Add(new Claim("RoleId", userCompany.RoleId.Value.ToString()));
        }

        // Tworzymy nową ClaimsIdentity i aktualizujemy autentykację
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Przekierowujemy do głównej strony
        return RedirectToAction("Index", "Main");
    }
}
