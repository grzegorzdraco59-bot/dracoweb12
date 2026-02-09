using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kompatybilność: przekierowanie do Kontrahenci
/// </summary>
[Authorize(Policy = "Kontrahenci:Read")]
public class SuppliersController : BaseController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return RedirectToAction("Index", "Kontrahenci");
    }
}
