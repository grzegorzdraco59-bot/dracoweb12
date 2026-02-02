using ERP.Application.Services;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Przeliczanie kwot pozycji (netto_poz, vat_poz, brutto_poz) i sumy brutto oferty (sum_brutto).
/// </summary>
public class OfferTotalsService : IOfferTotalsService
{
    private readonly DatabaseContext _context;

    public OfferTotalsService(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task RecalculateOfferLinesAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        // netto_poz = ROUND(ilosc * cena_netto * (1 - IFNULL(Rabat,0)/100), 2), vat_poz = ROUND(netto_poz * stawka_vat_pct/100, 2), brutto_poz = netto_poz + vat_poz
        // stawka_vat w DB to VARCHAR – parsowanie: zamiana ',' na '.', usunięcie '%'
        var cmd = new MySqlCommand(
            "UPDATE ofertypozycje p SET " +
            "p.netto_poz = ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2), " +
            "p.vat_poz = ROUND(ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2) * " +
            "COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100, 2), " +
            "p.brutto_poz = ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2) + " +
            "ROUND(ROUND(COALESCE(p.ilosc, 0) * COALESCE(p.cena_netto, 0) * (1 - IFNULL(p.Rabat, 0) / 100), 2) * " +
            "COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0) / 100, 2) " +
            "WHERE p.oferta_id = @OfferId",
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<decimal> RecalculateSumBruttoAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var selectCmd = new MySqlCommand(
            "SELECT COALESCE(SUM(brutto_poz), 0) FROM ofertypozycje WHERE oferta_id = @OfferId",
            connection);
        selectCmd.Parameters.AddWithValue("@OfferId", offerId);
        var sumBrutto = (decimal)(await selectCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0m);

        var updateCmd = new MySqlCommand(
            "UPDATE oferty SET sum_brutto = @SumBrutto WHERE id = @OfferId",
            connection);
        updateCmd.Parameters.AddWithValue("@OfferId", offerId);
        updateCmd.Parameters.AddWithValue("@SumBrutto", sumBrutto);
        await updateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return sumBrutto;
    }

    /// <inheritdoc />
    public async Task RecalcOfferTotalsAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "UPDATE oferty o " +
            "LEFT JOIN (" +
            "  SELECT oferta_id, " +
            "    COALESCE(SUM(netto_poz), 0) AS s_netto, " +
            "    COALESCE(SUM(vat_poz), 0) AS s_vat, " +
            "    COALESCE(SUM(brutto_poz), 0) AS s_brutto " +
            "  FROM ofertypozycje WHERE oferta_id = @OfferId GROUP BY oferta_id" +
            ") x ON x.oferta_id = o.id " +
            "SET o.sum_netto = COALESCE(x.s_netto, 0), o.sum_vat = COALESCE(x.s_vat, 0), o.sum_brutto = COALESCE(x.s_brutto, 0) " +
            "WHERE o.id = @OfferId",
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<decimal> GetSumBruttoAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT COALESCE(sum_brutto, 0) FROM oferty WHERE id = @OfferId",
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }
}
