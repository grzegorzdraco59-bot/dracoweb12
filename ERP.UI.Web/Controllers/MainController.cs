using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Główny kontroler aplikacji - wymaga autentykacji i wybranej firmy
/// Ochrona zapewniona przez BaseController
/// </summary>
[Authorize(Policy = "CompanyAccessAndRole")]
public class MainController : BaseController
{
    private readonly IUserContext _userContext;

    public MainController(IUserContext userContext)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public IActionResult Index()
    {
        var companyId = _userContext.CompanyId;
        ViewBag.CompanyId = companyId;
        return View();
    }

    /// <summary>
    /// Nawigacja do widoku na podstawie numeru (jak w MainViewModel.NavigateToView)
    /// </summary>
    public IActionResult Navigate(int id)
    {
        // Przekierowania do konkretnych kontrolerów
        return id switch
        {
            1 => RedirectToAction("Index", "Offers"), // Oferty
            8 => RedirectToAction("Index", "Suppliers"), // Dostawcy
            9 => RedirectToAction("Index", "Customers"), // Odbiorcy
            10 => RedirectToAction("Index", "Products"), // Towary
            11 => RedirectToAction("Index", "Orders"), // Zamówienia
            13 => RedirectToAction("Index", "OrdersHala"), // zamowienia hala
            23 => RedirectToAction("Index", "Admin"), // Admin
            _ => RedirectWithInfo(id)
        };
    }

    private IActionResult RedirectWithInfo(int id)
    {
        TempData["InfoMessage"] = $"Moduł (ID={id}) nie jest jeszcze zaimplementowany.";
        return RedirectToAction(nameof(Index));
    }
}
