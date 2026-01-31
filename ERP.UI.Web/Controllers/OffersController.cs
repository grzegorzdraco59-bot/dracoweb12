using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ERP.Application.Services;
using ERP.Domain.Enums;
using ERP.Domain.Repositories;
using ERP.Application.DTOs;
using ERP.UI.Web.Services;
using IUserContext = ERP.UI.Web.Services.IUserContext;

namespace ERP.UI.Web.Controllers;

/// <summary>
/// Kontroler do zarządzania ofertami
/// </summary>
[Authorize(Policy = "Offers:Read")]
public class OffersController : BaseController
{
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferPositionRepository _offerPositionRepository;
    private readonly IOfferService _offerService;
    private readonly IUserContext _userContext;

    public OffersController(IOfferRepository offerRepository, IOfferPositionRepository offerPositionRepository, IOfferService offerService, IUserContext userContext)
    {
        _offerRepository = offerRepository ?? throw new ArgumentNullException(nameof(offerRepository));
        _offerPositionRepository = offerPositionRepository ?? throw new ArgumentNullException(nameof(offerPositionRepository));
        _offerService = offerService ?? throw new ArgumentNullException(nameof(offerService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>
    /// Wyświetla listę ofert (Browse) - jak w OffersView.xaml
    /// </summary>
    public async Task<IActionResult> Index(string? searchText = null, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
        {
            return RedirectToAction("Select", "Company");
        }

        // Pobieramy wszystkie oferty dla wybranej firmy
        var offers = await _offerRepository.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        
        // Mapujemy do DTO z formatowaniem daty
        var offersDto = offers.Select(MapToDto).ToList();

        // Filtrowanie po SearchText (jak w OffersViewModel.FilterOffers)
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchTextLower = searchText.ToLowerInvariant();
            offersDto = offersDto.Where(offer =>
                offer.Id.ToString().Contains(searchTextLower) ||
                (offer.OfferNumber?.ToString().Contains(searchTextLower) ?? false) ||
                (offer.FormattedOfferDate?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                (offer.CustomerName?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                (offer.Currency?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                (offer.TotalBrutto?.ToString().Contains(searchTextLower) ?? false) ||
                (offer.Operator?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                (offer.CustomerStreet?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                (offer.CustomerCity?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                (offer.CustomerNip?.ToLowerInvariant().Contains(searchTextLower) ?? false)
            ).ToList();
        }

        ViewBag.SearchText = searchText;
        return View(offersDto);
    }

    /// <summary>
    /// Pobiera pozycje oferty (API endpoint dla AJAX) - jak w OffersViewModel.LoadOfferPositionsAsync
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPositions(int offerId, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
        {
            return Json(new { error = "Brak wybranej firmy" });
        }

        try
        {
            var positions = await _offerPositionRepository.GetByOfferIdAsync(offerId, cancellationToken);
            var positionsDto = positions.Select(MapPositionToDto).ToList();
            return Json(positionsDto);
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mapuje OfferPosition do OfferPositionDto (jak w OffersViewModel.MapToDto)
    /// </summary>
    private static OfferPositionDto MapPositionToDto(ERP.Domain.Entities.OfferPosition position)
    {
        return new OfferPositionDto
        {
            Id = position.Id,
            CompanyId = position.CompanyId,
            OfferId = position.OfferId,
            ProductId = position.ProductId,
            SupplierId = position.SupplierId,
            ProductCode = position.ProductCode,
            Name = position.Name,
            NameEng = position.NameEng,
            Unit = position.Unit,
            UnitEng = position.UnitEng,
            Quantity = position.Quantity,
            Price = position.Price,
            Discount = position.Discount,
            PriceAfterDiscount = position.PriceAfterDiscount,
            PriceAfterDiscountAndQuantity = position.PriceAfterDiscountAndQuantity,
            VatRate = position.VatRate,
            Vat = position.Vat,
            PriceBrutto = position.PriceBrutto,
            OfferNotes = position.OfferNotes,
            InvoiceNotes = position.InvoiceNotes,
            Other1 = position.Other1,
            GroupNumber = position.GroupNumber,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt
        };
    }

    /// <summary>
    /// Mapuje Offer do OfferDto z formatowaniem daty Clarion (jak w OffersViewModel.MapToDto)
    /// </summary>
    private static OfferDto MapToDto(ERP.Domain.Entities.Offer offer)
    {
        // Konwersja daty Clarion (liczba dni od 28.12.1800) na format dd/MM/yyyy
        string? formattedDate = null;
        if (offer.OfferDate.HasValue)
        {
            try
            {
                // Data bazowa Clarion: 28 grudnia 1800
                var baseDate = new DateTime(1800, 12, 28);
                var offerDateTime = baseDate.AddDays(offer.OfferDate.Value);
                formattedDate = offerDateTime.ToString("dd/MM/yyyy");
            }
            catch
            {
                formattedDate = $"Błąd: {offer.OfferDate.Value}";
            }
        }

        return new OfferDto
        {
            Id = offer.Id,
            CompanyId = offer.CompanyId,
            ForProforma = offer.ForProforma,
            ForOrder = offer.ForOrder,
            OfferDate = offer.OfferDate,
            FormattedOfferDate = formattedDate,
            OfferNumber = offer.OfferNumber,
            CustomerId = offer.CustomerId,
            CustomerName = offer.CustomerName,
            CustomerStreet = offer.CustomerStreet,
            CustomerPostalCode = offer.CustomerPostalCode,
            CustomerCity = offer.CustomerCity,
            CustomerCountry = offer.CustomerCountry,
            CustomerNip = offer.CustomerNip,
            CustomerEmail = offer.CustomerEmail,
            RecipientName = offer.CustomerName,
            Currency = offer.Currency,
            TotalPrice = offer.TotalPrice,
            VatRate = offer.VatRate,
            TotalVat = offer.TotalVat,
            TotalBrutto = offer.TotalBrutto,
            OfferNotes = offer.OfferNotes,
            AdditionalData = offer.AdditionalData,
            Operator = offer.Operator,
            TradeNotes = offer.TradeNotes,
            ForInvoice = offer.ForInvoice,
            History = offer.History,
            Status = offer.Status.ToString(),
            CreatedAt = offer.CreatedAt,
            UpdatedAt = offer.UpdatedAt
        };
    }

    /// <summary>
    /// Zmiana statusu oferty (FAZA4 – test: Draft→Sent, Sent→Accepted).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int offerId, string newStatus, CancellationToken cancellationToken = default)
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            return RedirectToAction("Select", "Company");

        if (!Enum.TryParse<OfferStatus>(newStatus, true, out var status))
            return RedirectToAction(nameof(Index), new { error = "Nieprawidłowy status" });

        try
        {
            await _offerService.SetStatusAsync(offerId, companyId.Value, status, cancellationToken);
            return RedirectToAction(nameof(Index), new { message = $"Status zmieniony na {status}" });
        }
        catch (ERP.Domain.Exceptions.BusinessRuleException ex)
        {
            return RedirectToAction(nameof(Index), new { error = ex.Message });
        }
    }
}
