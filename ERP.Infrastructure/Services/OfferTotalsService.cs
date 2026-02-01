using ERP.Application.Services;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Services;

/// <summary>
/// Przeliczanie oferty.sum_brutto z pozycji. oferty.sum_brutto = SUM(ofertypozycje.brutto_poz).
/// </summary>
public class OfferTotalsService : IOfferTotalsService
{
    private readonly DatabaseContext _context;

    public OfferTotalsService(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task RecalculateSumBruttoAsync(int offerId, CancellationToken cancellationToken = default)
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
    }
}
