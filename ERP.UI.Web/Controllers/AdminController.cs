using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler 23. Admin – okno w przygotowaniu
/// </summary>
[Authorize(Policy = "Admin")]
public class AdminController : BaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
