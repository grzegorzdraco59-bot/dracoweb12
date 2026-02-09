using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Application.Repositories;
using ERP.UI.Web.Services;
using IUserContext = ERP.UI.Web.Services.IUserContext;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler kontrahentów (Browse)
/// </summary>
[Authorize(Policy = "Kontrahenci:Read")]
public class KontrahenciController : BaseController
{
    private readonly IKontrahenciQueryRepository _repo;
    private readonly IUserContext _userContext;

    public KontrahenciController(IKontrahenciQueryRepository repo, IUserContext userContext)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        var kontrahenci = await _repo.GetAllForCompanyAsync(companyId.Value, cancellationToken);
        return View("~/Views/Customers/Index.cshtml", kontrahenci.ToList());
    }
}
