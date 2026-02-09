using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kompatybilność: przekierowanie do Kontrahenci
/// </summary>
[Authorize(Policy = "Kontrahenci:Read")]
public class CustomersController : BaseController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return RedirectToAction("Index", "Kontrahenci");
    }
}
