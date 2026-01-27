using Microsoft.AspNetCore.Mvc;
using ERP.Application.Services;
using ERP.Application.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AppAuthService = ERP.Application.Services.IAuthenticationService;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler obsługujący logowanie użytkowników
/// </summary>
public class AccountController : Controller
{
    private readonly AppAuthService _authenticationService;

    public AccountController(AppAuthService authenticationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
    }

    /// <summary>
    /// Wyświetla formularz logowania
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Przetwarza logowanie użytkownika
    /// Po sukcesie: ustawia Claims i przekierowuje do Company/Select
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string login, string password, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Login i hasło są wymagane.");
            return View();
        }

        // Autentykacja użytkownika
        var user = await _authenticationService.AuthenticateAsync(login, password, cancellationToken);

        if (user == null)
        {
            ModelState.AddModelError("", "Nieprawidłowy login lub hasło.");
            return View();
        }

        // Model mentalny: Autoryzacja = Identity / Claims
        // Tworzymy Claims dla zalogowanego użytkownika
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false, // Sesja tylko na czas przeglądarki
            AllowRefresh = true
        };

        // Logujemy użytkownika (ustawiamy Claims)
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Po sukcesie: redirect do returnUrl jeśli lokalny, w przeciwnym razie do wyboru firmy
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction("Select", "Company");
    }

    /// <summary>
    /// Wylogowanie użytkownika
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Czyszczenie sesji
        HttpContext.Session.Clear();
        
        // Wylogowanie (usunięcie Claims)
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        return RedirectToAction("Login", "Account");
    }
}
