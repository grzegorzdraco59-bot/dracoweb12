using System.Globalization;
using ERP.Application.DTOs;
using ERP.Application.Helpers;
using ERP.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Generuje PDF oferty – nagłówek OF/yyyy/MM/dd-N, dane i sumy z DTO (nie liczone w XAML).
/// </summary>
public class OfferPdfService : IOfferPdfService
{
    public Task GeneratePdfAsync(OfferDto offer, IReadOnlyList<OfferPositionDto> positions, Stream output, CompanyDto? company = null, CancellationToken cancellationToken = default)
    {
        var dataOferty = offer.DataOferty ?? (offer.OfferDate.HasValue && offer.OfferDate.Value > 0
            ? new DateTime(1800, 12, 28).AddDays(offer.OfferDate.Value)
            : DateTime.Today);
        var nrOferty = offer.NrOferty ?? 0;
        var offerNo = OfferNumberHelper.BuildOfferNo(dataOferty, nrOferty);

        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("OFERTA").Bold().FontSize(18).FontColor(Colors.Blue.Medium);
                    col.Item().Text(offerNo).SemiBold().FontSize(14);
                    if (company != null)
                    {
                        col.Item().PaddingTop(6).Text("Sprzedawca / Firma").SemiBold().FontSize(11);
                        if (!string.IsNullOrWhiteSpace(company.Name))
                            col.Item().Text(company.Name);
                        var addrParts = new[] { company.Street, company.PostalCode, company.City }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToList();
                        if (addrParts.Count > 0)
                            col.Item().Text(string.Join(", ", addrParts));
                        if (!string.IsNullOrWhiteSpace(company.Country))
                            col.Item().Text(company.Country);
                        if (!string.IsNullOrWhiteSpace(company.Nip))
                            col.Item().Text($"NIP: {company.Nip}");
                        if (!string.IsNullOrWhiteSpace(company.Phone1))
                            col.Item().Text($"Tel: {company.Phone1}");
                        if (!string.IsNullOrWhiteSpace(company.Email))
                            col.Item().Text($"E-mail: {company.Email}");
                    }
                    col.Item().PaddingTop(4).Text($"Data oferty: {dataOferty:yyyy-MM-dd}  |  Klient: {offer.CustomerName ?? "—"}");
                });

                page.Content().PaddingVertical(10).Column(content =>
                {
                    content.Spacing(8);
                    content.Item().Text($"Klient: {offer.CustomerName ?? "—"}").SemiBold();
                    if (!string.IsNullOrWhiteSpace(offer.CustomerStreet))
                        content.Item().Text($"Adres: {offer.CustomerStreet}, {offer.CustomerPostalCode} {offer.CustomerCity}");
                    if (!string.IsNullOrWhiteSpace(offer.CustomerNip))
                        content.Item().Text($"NIP: {offer.CustomerNip}");
                    content.Item().PaddingTop(8).Text("Pozycje:").SemiBold();

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(def =>
                        {
                            def.RelativeColumn(3);
                            def.ConstantColumn(50);
                            def.ConstantColumn(70);
                            def.ConstantColumn(50);
                            def.ConstantColumn(80);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Towar/Usługa").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).AlignRight().Text("Ilość").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).AlignRight().Text("Cena netto").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).AlignRight().Text("VAT").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).AlignRight().Text("Brutto").SemiBold();
                        });
                        foreach (var p in positions)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(p.Name ?? "—");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text((p.Ilosc ?? 0).ToString("N2", CultureInfo.CurrentCulture));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text((p.CenaNetto ?? 0).ToString("N2", CultureInfo.CurrentCulture));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text(p.VatRate ?? "—");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).AlignRight().Text((p.BruttoPoz ?? 0).ToString("N2", CultureInfo.CurrentCulture));
                        }
                    });

                    var sumNetto = offer.TotalPrice ?? 0;
                    var sumVat = offer.TotalVat ?? 0;
                    var sumBrutto = offer.SumBrutto ?? offer.TotalBrutto ?? 0;
                    content.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(220).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Razem netto: {sumNetto:N2} {offer.Currency ?? "PLN"}");
                            col.Item().AlignRight().Text($"Razem VAT: {sumVat:N2} {offer.Currency ?? "PLN"}");
                            col.Item().AlignRight().Text($"Razem brutto: {sumBrutto:N2} {offer.Currency ?? "PLN"}").Bold();
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Strona ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(output);

        return Task.CompletedTask;
    }
}
