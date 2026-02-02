using ERP.Application.DTOs;

namespace ERP.Application.Services;

/// <summary>
/// Generowanie PDF oferty – nagłówek z numerem OF/yyyy/MM/dd-N, dane firmy (Sprzedawca), dane z DTO (sumy z DB/logiki).
/// </summary>
public interface IOfferPdfService
{
    /// <summary>Generuje PDF oferty do strumienia. Numer w nagłówku: OF/yyyy/MM/dd-N. Opcjonalnie blok Sprzedawca/Firma pod tytułem.</summary>
    Task GeneratePdfAsync(OfferDto offer, IReadOnlyList<OfferPositionDto> positions, Stream output, CompanyDto? company = null, CancellationToken cancellationToken = default);
}
