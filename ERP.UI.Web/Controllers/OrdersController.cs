using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.UI.Web.Services;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler 11. Zamówienia (Browse zamowienia)
/// </summary>
[Authorize(Policy = "Orders:Read")]
public class OrdersController : BaseController
{
    private readonly IOrderMainRepository _orderMainRepository;
    private readonly IOrderPositionMainRepository _orderPositionMainRepository;
    private readonly IUserContext _userContext;

    public OrdersController(IOrderMainRepository orderMainRepository, IOrderPositionMainRepository orderPositionMainRepository, IUserContext userContext)
    {
        _orderMainRepository = orderMainRepository ?? throw new ArgumentNullException(nameof(orderMainRepository));
        _orderPositionMainRepository = orderPositionMainRepository ?? throw new ArgumentNullException(nameof(orderPositionMainRepository));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        var orders = await _orderMainRepository.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        return View(orders);
    }

    /// <summary>
    /// Pobiera pozycje zamówienia (API endpoint dla AJAX) - podobnie jak OffersController.GetPositions
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPositions(int orderId, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
        {
            return Json(new { error = "Brak wybranej firmy" });
        }

        try
        {
            var positions = await _orderPositionMainRepository.GetByOrderIdAsync(orderId, cancellationToken);
            return Json(positions);
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }
}
