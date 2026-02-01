using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Application.Repositories;
using ERP.Application.DTOs;
using ERP.UI.Web.Services;
using IUserContext = ERP.UI.Web.Services.IUserContext;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler listy faktur (Browse + pozycje) – wzorzec jak OffersController.
/// </summary>
[Authorize(Policy = "Invoices:Read")]
public class InvoicesController : BaseController
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoicePositionRepository _invoicePositionRepository;
    private readonly IUserContext _userContext;

    public InvoicesController(
        IInvoiceRepository invoiceRepository,
        IInvoicePositionRepository invoicePositionRepository,
        IUserContext userContext)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _invoicePositionRepository = invoicePositionRepository ?? throw new ArgumentNullException(nameof(invoicePositionRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Lista faktur (Browse) – jak Offers/Index: nagłówek, wyszukiwanie, tabela, pozycje pod spodem.
    /// </summary>
    public async Task<IActionResult> Index(string? searchText = null, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
        {
            return RedirectToAction("Select", "Company");
        }

        var invoices = await _invoiceRepository.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        var list = invoices.ToList();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchLower = searchText.ToLowerInvariant();
            list = list.Where(inv =>
                inv.Id.ToString().Contains(searchLower) ||
                (inv.FormattedDataFaktury?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (inv.SkrotNazwaFaktury?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (inv.NrFakturyText?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (inv.NrFaktury?.ToString().Contains(searchLower) ?? false) ||
                (inv.OdbiorcaNazwa?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (inv.Waluta?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (inv.KwotaBrutto?.ToString().Contains(searchLower) ?? false) ||
                (inv.Operator?.ToLowerInvariant().Contains(searchLower) ?? false)
            ).ToList();
        }

        ViewBag.SearchText = searchText;
        return View(list);
    }

    /// <summary>
    /// Pozycje faktury (AJAX) – jak Offers/GetPositions.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPositions(int invoiceId, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
        {
            return Json(new { error = "Brak wybranej firmy" });
        }

        try
        {
            var positions = await _invoicePositionRepository.GetByInvoiceIdAsync(invoiceId, cancellationToken);
            return Json(positions.ToList());
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }
}
