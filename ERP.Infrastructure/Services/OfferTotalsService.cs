using ERP.Application.Services;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Przeliczanie kwot pozycji (netto_poz, vat_poz, brutto_poz) i sumy brutto aoferty (sum_brutto).
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
        // Mapowanie docelowe: Cena_po_rabacie_i_sztukach, vat, cena_brutto. Źródło: COALESCE(ilosc,Sztuki), COALESCE(cena_netto,Cena).
        var iloscExpr = "COALESCE(p.ilosc, p.Sztuki, 0)";
        var cenaExpr = "COALESCE(p.cena_netto, p.Cena, 0)";
        var stawkaExpr = "COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0)";
        var nettoExpr = $"ROUND({iloscExpr} * {cenaExpr} * (1 - IFNULL(p.Rabat, 0) / 100), 2)";
        var vatExpr = $"ROUND({nettoExpr} * {stawkaExpr} / 100, 2)";
        var bruttoExpr = $"ROUND({nettoExpr} + {vatExpr}, 2)";
        var whereClause = "WHERE COALESCE(p.oferta_id, p.ID_oferta) = @OfferId";

        var cmd = new MySqlCommand(
            "UPDATE apozycjeoferty p SET " +
            "p.Cena_po_rabacie_i_sztukach = " + nettoExpr + ", " +
            "p.vat = " + vatExpr + ", " +
            "p.cena_brutto = " + bruttoExpr + " " +
            whereClause,
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<decimal> RecalculateSumBruttoAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var selectCmd = new MySqlCommand(
            "SELECT COALESCE(SUM(COALESCE(brutto_poz, cena_brutto)), 0) FROM apozycjeoferty WHERE COALESCE(oferta_id, ID_oferta) = @OfferId",
            connection);
        selectCmd.Parameters.AddWithValue("@OfferId", offerId);
        var sumBrutto = (decimal)(await selectCmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false) ?? 0m);

        var updateCmd = new MySqlCommand(
            "UPDATE aoferty SET sum_brutto = @SumBrutto WHERE id_oferta = @OfferId",
            connection);
        updateCmd.Parameters.AddWithValue("@OfferId", offerId);
        updateCmd.Parameters.AddWithValue("@SumBrutto", sumBrutto);
        await updateCmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        return sumBrutto;
    }

    /// <inheritdoc />
    public async Task RecalcOfferTotalsAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await RecalculateOfferTotalsAsync(offerId, cancellationToken);
    }

    /// <summary>
    /// Przelicza sum_netto, sum_vat, sum_brutto nagłówka oferty na podstawie pozycji.
    /// Formuła: suma_netto = SUM(cena_netto * ilosc), suma_vat = SUM((cena_netto * ilosc) * stawka_vat / 100), suma_brutto = suma_netto + suma_vat.
    /// Oferta bez pozycji → sumy = 0.
    /// </summary>
    public async Task RecalculateOfferTotalsAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var stawkaVatExpr = "COALESCE(CAST(REPLACE(REPLACE(TRIM(REPLACE(IFNULL(p.stawka_vat, '0'), '%', '')), ',', '.'), ' ', '') AS DECIMAL(10,4)), 0)";
        var iloscExpr = "COALESCE(p.ilosc, p.Sztuki, 0)";
        var cenaExpr = "COALESCE(p.cena_netto, p.Cena, 0)";
        var nettoExpr = iloscExpr + " * " + cenaExpr;
        var sql =
            "UPDATE aoferty o " +
            "LEFT JOIN (" +
            "  SELECT COALESCE(p.oferta_id, p.ID_oferta) AS oferta_id, " +
            "    ROUND(SUM(" + nettoExpr + "), 2) AS s_netto, " +
            "    ROUND(SUM(" + nettoExpr + " * " + stawkaVatExpr + " / 100), 2) AS s_vat " +
            "  FROM apozycjeoferty p WHERE COALESCE(p.oferta_id, p.ID_oferta) = @OfferId GROUP BY COALESCE(p.oferta_id, p.ID_oferta)" +
            ") x ON x.oferta_id = o.id_oferta " +
            "SET o.sum_netto = COALESCE(x.s_netto, 0), o.sum_vat = COALESCE(x.s_vat, 0), o.sum_brutto = COALESCE(x.s_netto, 0) + COALESCE(x.s_vat, 0) " +
            "WHERE o.id_oferta = @OfferId";
        var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<decimal> GetSumBruttoAsync(int offerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var cmd = new MySqlCommand(
            "SELECT COALESCE(total_brutto, 0) FROM aoferty_V WHERE id = @OfferId",
            connection);
        cmd.Parameters.AddWithValue("@OfferId", offerId);
        var result = await cmd.ExecuteScalarWithDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }
}
