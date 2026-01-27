using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Implementacja repozytorium Oferty (Offer) używająca MySqlConnector
/// Repozytoria zawierają tylko operacje CRUD - logika biznesowa jest w warstwie Application
/// </summary>
public class OfferRepository : IOfferRepository
{
    private readonly DatabaseContext _context;

    public OfferRepository(DatabaseContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Offer?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_oferta, id_firmy, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, " +
            "odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, " +
            "odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat, " +
            "total_vat, total_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi, " +
            "do_faktury, historia " +
            "FROM aoferty WHERE ID_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToOffer(reader);
        }

        return null;
    }

    public async Task<IEnumerable<Offer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Użyj GetByCompanyIdAsync zamiast GetAllAsync. Repozytoria wymagają companyId jako parametru.");
    }

    public async Task<IEnumerable<Offer>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var offers = new List<Offer>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT ID_oferta, id_firmy, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, " +
            "odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, " +
            "odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat, " +
            "total_vat, total_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi, " +
            "do_faktury, historia " +
            "FROM aoferty WHERE id_firmy = @CompanyId ORDER BY Data_oferty DESC, Nr_oferty DESC",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            offers.Add(MapToOffer(reader));
        }

        return offers;
    }

    public async Task<int> AddAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "INSERT INTO aoferty (id_firmy, do_proformy, do_zlecenia, Data_oferty, Nr_oferty, " +
            "odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, " +
            "odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat, " +
            "total_vat, total_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi, " +
            "do_faktury, historia) " +
            "VALUES (@CompanyId, @ForProforma, @ForOrder, @OfferDate, @OfferNumber, @CustomerId, " +
            "@CustomerName, @CustomerStreet, @CustomerPostalCode, @CustomerCity, @CustomerCountry, " +
            "@CustomerNip, @CustomerEmail, @Currency, @TotalPrice, @VatRate, @TotalVat, @TotalBrutto, " +
            "@OfferNotes, @AdditionalData, @Operator, @TradeNotes, @ForInvoice, @History); " +
            "SELECT LAST_INSERT_ID();",
            connection);
        
        AddOfferParameters(command, offer);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task UpdateAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "UPDATE aoferty SET " +
            "do_proformy = @ForProforma, do_zlecenia = @ForOrder, Data_oferty = @OfferDate, " +
            "Nr_oferty = @OfferNumber, odbiorca_ID_odbiorcy = @CustomerId, odbiorca_nazwa = @CustomerName, " +
            "odbiorca_ulica = @CustomerStreet, odbiorca_kod_poczt = @CustomerPostalCode, " +
            "odbiorca_miasto = @CustomerCity, odbiorca_panstwo = @CustomerCountry, odbiorca_nip = @CustomerNip, " +
            "odbiorca_mail = @CustomerEmail, Waluta = @Currency, Cena_calkowita = @TotalPrice, " +
            "stawka_vat = @VatRate, total_vat = @TotalVat, total_brutto = @TotalBrutto, " +
            "uwagi_do_oferty = @OfferNotes, dane_dodatkowe = @AdditionalData, operator = @Operator, " +
            "uwagi_targi = @TradeNotes, do_faktury = @ForInvoice, historia = @History " +
            "WHERE ID_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        
        command.Parameters.AddWithValue("@Id", offer.Id);
        AddOfferParameters(command, offer);
        
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM aoferty WHERE ID_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COUNT(1) FROM aoferty WHERE ID_oferta = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task<int?> GetNextOfferNumberForDateAsync(int offerDate, int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "SELECT COALESCE(MAX(Nr_oferty), 0) + 1 " +
            "FROM aoferty " +
            "WHERE id_firmy = @CompanyId AND Data_oferty = @OfferDate",
            connection);
        command.Parameters.AddWithValue("@CompanyId", companyId);
        command.Parameters.AddWithValue("@OfferDate", offerDate);
        
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return 1;
        
        return Convert.ToInt32(result);
    }

    private static Offer MapToOffer(MySqlDataReader reader)
    {
        int id = reader.GetInt32(reader.GetOrdinal("ID_oferta"));
        int companyId = reader.GetInt32(reader.GetOrdinal("id_firmy"));
        string @operator = reader.GetString(reader.GetOrdinal("operator"));
        
        var offer = new Offer(companyId, @operator);
        
        // Użyj refleksji do ustawienia Id i innych właściwości
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProperty?.SetValue(offer, id);
        
        offer.UpdateOfferInfo(
            GetNullableInt(reader, "Data_oferty"),
            GetNullableInt(reader, "Nr_oferty"),
            GetNullableString(reader, "Waluta")
        );
        
        offer.UpdateCustomerInfo(
            GetNullableInt(reader, "odbiorca_ID_odbiorcy"),
            GetNullableString(reader, "odbiorca_nazwa"),
            GetNullableString(reader, "odbiorca_ulica"),
            GetNullableString(reader, "odbiorca_kod_poczt"),
            GetNullableString(reader, "odbiorca_miasto"),
            GetNullableString(reader, "odbiorca_panstwo"),
            GetNullableString(reader, "odbiorca_nip"),
            GetNullableString(reader, "odbiorca_mail")
        );
        
        offer.UpdatePricing(
            GetNullableDecimal(reader, "Cena_calkowita"),
            GetNullableDecimal(reader, "stawka_vat"),
            GetNullableDecimal(reader, "total_vat"),
            GetNullableDecimal(reader, "total_brutto")
        );
        
        offer.UpdateFlags(
            GetNullableBool(reader, "do_proformy"),
            GetNullableBool(reader, "do_zlecenia"),
            reader.GetBoolean(reader.GetOrdinal("do_faktury"))
        );
        
        offer.UpdateNotes(
            GetNullableString(reader, "uwagi_do_oferty"),
            GetNullableString(reader, "dane_dodatkowe"),
            reader.GetString(reader.GetOrdinal("uwagi_targi"))
        );
        
        offer.UpdateHistory(reader.GetString(reader.GetOrdinal("historia")));
        
        return offer;
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static bool? GetNullableBool(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    }

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        int ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static void AddOfferParameters(MySqlCommand command, Offer offer)
    {
        command.Parameters.AddWithValue("@CompanyId", offer.CompanyId);
        command.Parameters.AddWithValue("@ForProforma", offer.ForProforma ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ForOrder", offer.ForOrder ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferDate", offer.OfferDate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferNumber", offer.OfferNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerId", offer.CustomerId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerName", offer.CustomerName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerStreet", offer.CustomerStreet ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerPostalCode", offer.CustomerPostalCode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerCity", offer.CustomerCity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerCountry", offer.CustomerCountry ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerNip", offer.CustomerNip ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CustomerEmail", offer.CustomerEmail ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Currency", offer.Currency ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TotalPrice", offer.TotalPrice ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@VatRate", offer.VatRate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TotalVat", offer.TotalVat ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TotalBrutto", offer.TotalBrutto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OfferNotes", offer.OfferNotes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@AdditionalData", offer.AdditionalData ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Operator", offer.Operator);
        command.Parameters.AddWithValue("@TradeNotes", offer.TradeNotes);
        command.Parameters.AddWithValue("@ForInvoice", offer.ForInvoice);
        command.Parameters.AddWithValue("@History", offer.History);
    }
}
