using ERP.Application.DTOs;
using ERP.Application.Services;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Services;
using MySqlConnector;

namespace ERP.Infrastructure.Repositories;

/// <summary>
/// Repozytorium towarów: SELECT z towary_V (alias id), INSERT/UPDATE do towary.
/// </summary>
public class ProductRepository
{
    private const string SelectColumns = "t.id, t.id_firmy, t.grupa, t.grupa_remanentu, t.status_towaru, " +
        "t.Nazwa_PL_draco, t.Nazwa_PL, t.Nazwa_ENG, t.Cena_PLN, t.Cena_EUR, t.Cena_USD, t.Waga_Kg, t.roboczogodziny, " +
        "t.Uwagi, t.dostawca, t.ilosc_magazyn, t.jednostki_zakupu, t.jednostki_sprzedazy, t.jednostka, " +
        "t.przelicznik_m_kg, t.cena_zakupu, t.waluta_zakupu, t.kurs_waluty, t.cena_zakupu_PLN, t.cena_zakupu_PLN_nowe_jednostki, " +
        "t.koszty_materialow, t.grupa_gtu, t.stawka_vat, t.jednostki_en, t.data_zakupu, t.ilosc_w_opakowaniu, " +
        "t.linia_produkcyjna, t.id_dostawcy, t.do_magazynu, t.cena_data, t.etykieta_nazwa, t.etykieta_wielkosc, t.ilosc_jednostkowa";

    private readonly DatabaseContext _context;
    private readonly IUserContext _userContext;
    private readonly IIdGenerator _idGenerator;

    public ProductRepository(DatabaseContext context, IUserContext userContext, IIdGenerator idGenerator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    private int GetCurrentCompanyId()
    {
        var companyId = _userContext.CompanyId;
        if (!companyId.HasValue)
            throw new InvalidOperationException("Brak wybranej firmy. Użytkownik musi być zalogowany i wybrać firmę.");
        return companyId.Value;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = new List<ProductDto>();
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            $"SELECT {SelectColumns} FROM towary_V t WHERE t.id_firmy = @CompanyId ORDER BY t.Nazwa_PL",
            connection);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(MapToProduct(reader));
        }

        return products;
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            $"SELECT {SelectColumns} FROM towary_V t WHERE t.id = @Id LIMIT 1",
            connection);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderWithDiagnosticsAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToProduct(reader);
        }

        return null;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var command = new MySqlCommand(
            "DELETE FROM towary WHERE id = @Id AND id_firmy = @CompanyId",
            connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@CompanyId", GetCurrentCompanyId());
        await command.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    /// <summary>
    /// Zapisuje towar: Update jeśli Id > 0, Insert jeśli Id == 0.
    /// </summary>
    public async Task<int> SaveAsync(ProductDto product, CancellationToken cancellationToken = default)
    {
        var companyId = GetCurrentCompanyId();

        if (product.Id > 0)
        {
            await UpdateAsync(product, companyId, cancellationToken);
            return product.Id;
        }

        return await InsertAsync(product, companyId, cancellationToken);
    }

    private async Task UpdateAsync(ProductDto p, int companyId, CancellationToken cancellationToken)
    {
        await using var connection = await _context.CreateConnectionAsync();
        var sql = "UPDATE towary SET " +
            "id_firmy=@id_firmy, grupa=@grupa, grupa_remanentu=@grupa_remanentu, status_towaru=@status_towaru, " +
            "Nazwa_PL_draco=@Nazwa_PL_draco, Nazwa_PL=@Nazwa_PL, Nazwa_ENG=@Nazwa_ENG, " +
            "Cena_PLN=@Cena_PLN, Cena_EUR=@Cena_EUR, Cena_USD=@Cena_USD, Waga_Kg=@Waga_Kg, roboczogodziny=@roboczogodziny, " +
            "Uwagi=@Uwagi, dostawca=@dostawca, ilosc_magazyn=@ilosc_magazyn, " +
            "jednostki_zakupu=@jednostki_zakupu, jednostki_sprzedazy=@jednostki_sprzedazy, jednostka=@jednostka, " +
            "przelicznik_m_kg=@przelicznik_m_kg, cena_zakupu=@cena_zakupu, waluta_zakupu=@waluta_zakupu, kurs_waluty=@kurs_waluty, " +
            "cena_zakupu_PLN=@cena_zakupu_PLN, cena_zakupu_PLN_nowe_jednostki=@cena_zakupu_PLN_nowe_jednostki, " +
            "koszty_materialow=@koszty_materialow, grupa_gtu=@grupa_gtu, stawka_vat=@stawka_vat, jednostki_en=@jednostki_en, " +
            "data_zakupu=@data_zakupu, ilosc_w_opakowaniu=@ilosc_w_opakowaniu, linia_produkcyjna=@linia_produkcyjna, " +
            "id_dostawcy=@id_dostawcy, do_magazynu=@do_magazynu, cena_data=@cena_data, " +
            "etykieta_nazwa=@etykieta_nazwa, etykieta_wielkosc=@etykieta_wielkosc, ilosc_jednostkowa=@ilosc_jednostkowa " +
            "WHERE id_towar=@id_towar";

        var cmd = new MySqlCommand(sql, connection);
        AddProductParameters(cmd, p, companyId);
        cmd.Parameters.AddWithValue("@id_towar", p.Id);

        await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
    }

    private async Task<int> InsertAsync(ProductDto p, int companyId, CancellationToken cancellationToken)
    {
        var connection = await _context.CreateConnectionAsync();
        await using var _ = connection;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var newId = (int)await _idGenerator.GetNextIdAsync("towary", connection, transaction, cancellationToken);

        var sql = "INSERT INTO towary (id_towar, id_firmy, grupa, grupa_remanentu, status_towaru, " +
            "Nazwa_PL_draco, Nazwa_PL, Nazwa_ENG, Cena_PLN, Cena_EUR, Cena_USD, Waga_Kg, roboczogodziny, " +
            "Uwagi, dostawca, ilosc_magazyn, jednostki_zakupu, jednostki_sprzedazy, jednostka, " +
            "przelicznik_m_kg, cena_zakupu, waluta_zakupu, kurs_waluty, cena_zakupu_PLN, cena_zakupu_PLN_nowe_jednostki, " +
            "koszty_materialow, grupa_gtu, stawka_vat, jednostki_en, data_zakupu, ilosc_w_opakowaniu, " +
            "linia_produkcyjna, id_dostawcy, do_magazynu, cena_data, etykieta_nazwa, etykieta_wielkosc, ilosc_jednostkowa) " +
            "VALUES (@id_towar, @id_firmy, @grupa, @grupa_remanentu, @status_towaru, " +
            "@Nazwa_PL_draco, @Nazwa_PL, @Nazwa_ENG, @Cena_PLN, @Cena_EUR, @Cena_USD, @Waga_Kg, @roboczogodziny, " +
            "@Uwagi, @dostawca, @ilosc_magazyn, @jednostki_zakupu, @jednostki_sprzedazy, @jednostka, " +
            "@przelicznik_m_kg, @cena_zakupu, @waluta_zakupu, @kurs_waluty, @cena_zakupu_PLN, @cena_zakupu_PLN_nowe_jednostki, " +
            "@koszty_materialow, @grupa_gtu, @stawka_vat, @jednostki_en, @data_zakupu, @ilosc_w_opakowaniu, " +
            "@linia_produkcyjna, @id_dostawcy, @do_magazynu, @cena_data, @etykieta_nazwa, @etykieta_wielkosc, @ilosc_jednostkowa)";

        try
        {
            var cmd = new MySqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@id_towar", newId);
            AddProductParameters(cmd, p, companyId);

            await cmd.ExecuteNonQueryWithDiagnosticsAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void AddProductParameters(MySqlCommand cmd, ProductDto p, int companyId)
    {
        cmd.Parameters.AddWithValue("@id_firmy", (object?)p.CompanyId ?? companyId);
        cmd.Parameters.AddWithValue("@grupa", p.Group ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@grupa_remanentu", p.GrupaRemanentu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@status_towaru", p.StatusTowaru ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Nazwa_PL_draco", p.NazwaPLdraco ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Nazwa_PL", p.NazwaPL ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Nazwa_ENG", p.NazwaENG ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Cena_PLN", p.Cena_PLN ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Cena_EUR", p.Cena_EUR ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Cena_USD", p.Cena_USD ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Waga_Kg", p.Waga_Kg ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@roboczogodziny", p.Roboczogodziny ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Uwagi", p.Uwagi ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dostawca", p.Dostawca ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ilosc_magazyn", p.IloscMagazyn ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@jednostki_zakupu", p.JednostkiZakupu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@jednostki_sprzedazy", p.JednostkiSprzedazy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@jednostka", p.Jednostka ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@przelicznik_m_kg", p.PrzelicznikMKg ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cena_zakupu", p.CenaZakupu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@waluta_zakupu", p.WalutaZakupu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@kurs_waluty", p.KursWaluty ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cena_zakupu_PLN", p.CenaZakupuPLN ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cena_zakupu_PLN_nowe_jednostki", p.CenaZakupuPLNNoweJednostki ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@koszty_materialow", p.KosztyMaterialow ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@grupa_gtu", p.GrupaGtu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@stawka_vat", p.StawkaVat ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@jednostki_en", p.JednostkiEn ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@data_zakupu", p.DataZakupu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ilosc_w_opakowaniu", p.IloscWOpakowaniu ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@linia_produkcyjna", p.LiniaProdukcyjna ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@id_dostawcy", p.IdDostawcy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@do_magazynu", p.DoMagazynu.HasValue ? (p.DoMagazynu.Value ? 1 : 0) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cena_data", p.CenaData ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@etykieta_nazwa", p.EtykietaNazwa ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@etykieta_wielkosc", p.EtykietaWielkosc ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ilosc_jednostkowa", p.IloscJednostkowa ?? (object)DBNull.Value);
    }

    private static ProductDto MapToProduct(MySqlDataReader reader)
    {
        return new ProductDto
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            CompanyId = GetNullableInt(reader, "id_firmy"),
            Group = GetNullableString(reader, "grupa"),
            GrupaRemanentu = GetNullableString(reader, "grupa_remanentu"),
            StatusTowaru = GetNullableString(reader, "status_towaru"),
            NazwaPLdraco = GetNullableString(reader, "Nazwa_PL_draco"),
            NazwaPL = GetNullableString(reader, "Nazwa_PL"),
            NazwaENG = GetNullableString(reader, "Nazwa_ENG"),
            Cena_PLN = GetNullableDecimal(reader, "Cena_PLN"),
            Cena_EUR = GetNullableDecimal(reader, "Cena_EUR"),
            Cena_USD = GetNullableDecimal(reader, "Cena_USD"),
            Waga_Kg = GetNullableDecimal(reader, "Waga_Kg"),
            Roboczogodziny = GetNullableDecimal(reader, "roboczogodziny"),
            Uwagi = GetNullableString(reader, "Uwagi"),
            Dostawca = GetNullableString(reader, "dostawca"),
            IloscMagazyn = GetNullableDecimal(reader, "ilosc_magazyn"),
            JednostkiZakupu = GetNullableString(reader, "jednostki_zakupu"),
            JednostkiSprzedazy = GetNullableString(reader, "jednostki_sprzedazy"),
            Jednostka = GetNullableString(reader, "jednostka"),
            PrzelicznikMKg = GetNullableDecimal(reader, "przelicznik_m_kg"),
            CenaZakupu = GetNullableDecimal(reader, "cena_zakupu"),
            WalutaZakupu = GetNullableString(reader, "waluta_zakupu"),
            KursWaluty = GetNullableDecimal(reader, "kurs_waluty"),
            CenaZakupuPLN = GetNullableDecimal(reader, "cena_zakupu_PLN"),
            CenaZakupuPLNNoweJednostki = GetNullableDecimal(reader, "cena_zakupu_PLN_nowe_jednostki"),
            KosztyMaterialow = GetNullableDecimal(reader, "koszty_materialow"),
            GrupaGtu = GetNullableString(reader, "grupa_gtu"),
            StawkaVat = GetNullableString(reader, "stawka_vat"),
            JednostkiEn = GetNullableString(reader, "jednostki_en"),
            DataZakupu = GetNullableInt(reader, "data_zakupu"),
            IloscWOpakowaniu = GetNullableDecimal(reader, "ilosc_w_opakowaniu"),
            LiniaProdukcyjna = GetNullableInt(reader, "linia_produkcyjna"),
            IdDostawcy = GetNullableInt(reader, "id_dostawcy"),
            DoMagazynu = GetNullableBool(reader, "do_magazynu"),
            CenaData = GetNullableInt(reader, "cena_data"),
            EtykietaNazwa = GetNullableString(reader, "etykieta_nazwa"),
            EtykietaWielkosc = GetNullableString(reader, "etykieta_wielkosc"),
            IloscJednostkowa = GetNullableDecimal(reader, "ilosc_jednostkowa")
        };
    }

    private static string? GetNullableString(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static decimal? GetNullableDecimal(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static bool? GetNullableBool(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal)) return null;
        var val = reader.GetValue(ordinal);
        if (val is bool b) return b;
        if (val is byte byteVal) return byteVal != 0;
        if (val is int intVal) return intVal != 0;
        return null;
    }
}
