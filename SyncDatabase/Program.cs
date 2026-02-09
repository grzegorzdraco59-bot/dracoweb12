using MySqlConnector;

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;";

// Tryb ETAP 1 - analiza tabel pod migrację PK → id
if (args.Length > 0 && args[0].Equals("etap1", StringComparison.OrdinalIgnoreCase))
{
    await RunEtap1Analysis(connectionString);
    return;
}
// Tryb raport - klasy FK/PK (bez zmian w DB)
if (args.Length > 0 && args[0].Equals("raport", StringComparison.OrdinalIgnoreCase))
{
    await RunFkPkReport(connectionString);
    return;
}
// Tryb ETAP 2 - migracja PK → id (KLASA B: incoming=0, outgoing>0)
if (args.Length > 0 && args[0].Equals("etap2", StringComparison.OrdinalIgnoreCase))
{
    await RunEtap2Migration(connectionString);
    return;
}
// Tryb ETAP 3 - analiza KLASA C (incoming>0, rdzeń)
if (args.Length > 0 && args[0].Equals("etap3", StringComparison.OrdinalIgnoreCase))
{
    await RunEtap3Analysis(connectionString);
    return;
}

Console.WriteLine("=== Synchronizacja struktury bazy danych locbd z projektem ===\n");

// Mapowanie tabel do modeli domenowych
var tableModelMapping = new Dictionary<string, string>
{
    { "odbiorcy", "Customer" },
    { "zamowieniahala", "Order" },
    { "operator", "User" },
    { "operator_login", "UserLogin" },
    { "operatorfirma", "UserCompany" },
    { "firmy", "Company" },
    { "rola", "Role" },
    { "aoferty", "Offer" },
    { "apozycjeoferty", "OfferPosition" },
    { "operator_table_permissions", "OperatorTablePermission" }
};

try
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ Połączenie z bazą danych OK!\n");

    // Pobierz listę wszystkich tabel
    var tablesCommand = new MySqlCommand(
        "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'locbd' ORDER BY TABLE_NAME",
        connection);
    
    var tables = new List<string>();
    using (var reader = await tablesCommand.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
    }

    Console.WriteLine($"Znaleziono {tables.Count} tabel w bazie danych:\n");

    var tableStructures = new Dictionary<string, List<ColumnInfo>>();

    foreach (var tableName in tables)
    {
        Console.WriteLine($"Sprawdzanie tabeli: {tableName}");
        
        var describeCommand = new MySqlCommand($"DESCRIBE `{tableName}`", connection);
        var columns = new List<ColumnInfo>();
        
        using (var reader = await describeCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    Type = reader.GetString(1),
                    Nullable = reader.GetString(2) == "YES",
                    Key = reader.GetString(3),
                    Default = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Extra = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
        }
        
        tableStructures[tableName] = columns;
        Console.WriteLine($"  ✓ Znaleziono {columns.Count} kolumn\n");
    }

    // Wyświetl szczegółową strukturę dla głównych tabel
    Console.WriteLine("\n=== Szczegółowa struktura głównych tabel ===\n");
    
    foreach (var tableName in tableModelMapping.Keys)
    {
        if (!tableStructures.ContainsKey(tableName))
        {
            Console.WriteLine($"⚠ Tabela '{tableName}' nie istnieje w bazie danych\n");
            continue;
        }

        Console.WriteLine($"Tabela: {tableName} (Model: {tableModelMapping[tableName]})");
        Console.WriteLine("=".PadRight(100, '='));
        Console.WriteLine($"{"Kolumna",-35} {"Typ",-25} {"Null",-8} {"Klucz",-8} {"Domyślna",-15}");
        Console.WriteLine("-".PadRight(100, '-'));
        
        foreach (var column in tableStructures[tableName])
        {
            var nullable = column.Nullable ? "YES" : "NO";
            var defaultValue = column.Default ?? "NULL";
            Console.WriteLine($"{column.Name,-35} {column.Type,-25} {nullable,-8} {column.Key,-8} {defaultValue,-15}");
        }
        
        Console.WriteLine();
    }

    // Zapisz strukturę do pliku
    var outputFile = "database_structure.txt";
    await File.WriteAllTextAsync(outputFile, GenerateStructureReport(tableStructures, tableModelMapping));
    Console.WriteLine($"✓ Raport zapisany do pliku: {outputFile}\n");

    // Porównaj strukturę z kodem
    Console.WriteLine("=== Porównanie z kodem ===\n");
    var differences = CompareWithCode(tableStructures, connection);
    
    if (differences.Any())
    {
        Console.WriteLine($"⚠ Znaleziono {differences.Count} różnic:\n");
        foreach (var diff in differences)
        {
            Console.WriteLine($"  - {diff}");
        }
        
        var diffFile = "database_sync_differences.txt";
        await File.WriteAllTextAsync(diffFile, 
            $"Raport różnic - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
            string.Join("\n", differences));
        Console.WriteLine($"\n✓ Raport różnic zapisany do: {diffFile}");
    }
    else
    {
        Console.WriteLine("✓ Wszystkie sprawdzone tabele są zsynchronizowane z kodem!\n");
    }

    Console.WriteLine("=== Synchronizacja zakończona ===\n");
    Console.WriteLine("Sprawdź plik database_structure.txt aby zobaczyć pełną strukturę.");
    Console.WriteLine("Porównaj z modelami w ERP.Domain.Entities i zaktualizuj je jeśli potrzeba.\n");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Błąd: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Wewnętrzny błąd: {ex.InnerException.Message}");
    }
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nNaciśnij Enter, aby zakończyć...");
Console.ReadLine();

static async Task RunEtap1Analysis(string connectionString)
{
    try
    {
        using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();

        var tablesWithFkOut = new HashSet<string>();
        var tablesWithFkIn = new HashSet<string>();
        var allTablesPk = new List<(string Table, string PkColumn, string DataType, string FullType, bool AutoIncrement, int PkColumns)>();

        var cmd1 = new MySqlCommand(@"
            SELECT t.TABLE_NAME, k.COLUMN_NAME, c.DATA_TYPE, c.COLUMN_TYPE,
                   IF(c.EXTRA LIKE '%auto_increment%', 1, 0) as ai,
                   (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE k2 
                    WHERE k2.TABLE_SCHEMA=t.TABLE_SCHEMA AND k2.TABLE_NAME=t.TABLE_NAME 
                    AND k2.CONSTRAINT_NAME='PRIMARY') as pk_cols
            FROM information_schema.TABLES t
            JOIN information_schema.KEY_COLUMN_USAGE k 
                ON t.TABLE_SCHEMA=k.TABLE_SCHEMA AND t.TABLE_NAME=k.TABLE_NAME AND k.CONSTRAINT_NAME='PRIMARY'
            JOIN information_schema.COLUMNS c 
                ON c.TABLE_SCHEMA=k.TABLE_SCHEMA AND c.TABLE_NAME=k.TABLE_NAME AND c.COLUMN_NAME=k.COLUMN_NAME
            WHERE t.TABLE_SCHEMA='locbd' AND t.TABLE_TYPE='BASE TABLE'
            ORDER BY t.TABLE_NAME", conn);
        using (var r1 = await cmd1.ExecuteReaderAsync())
        {
            while (await r1.ReadAsync())
            {
                allTablesPk.Add((r1.GetString(0), r1.GetString(1), r1.GetString(2), r1.GetString(3),
                    r1.GetInt32(4) == 1, r1.GetInt32(5)));
            }
        }

        var cmd2 = new MySqlCommand(@"
            SELECT TABLE_NAME, REFERENCED_TABLE_NAME 
            FROM information_schema.REFERENTIAL_CONSTRAINTS 
            WHERE CONSTRAINT_SCHEMA='locbd'", conn);
        using (var r2 = await cmd2.ExecuteReaderAsync())
        {
            while (await r2.ReadAsync())
            {
                tablesWithFkOut.Add(r2.GetString(0));
                tablesWithFkIn.Add(r2.GetString(1));
            }
        }

        var skipTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ar_internal_metadata", "schema_migrations", "pliksql",
            "secwin_access", "secwin_access_copy", "secwin_counters", "secwin_licence4",
            "secwin_namecodes", "secwin_operators5", "secwin_operatorsusergroups",
            "v_operator_permissions_summary"
        };

        var stage1 = allTablesPk
            .Where(x => !skipTables.Contains(x.Table))
            .Where(x => x.PkColumns == 1)
            .Where(x => !tablesWithFkOut.Contains(x.Table))
            .Where(x => !tablesWithFkIn.Contains(x.Table))
            .Where(x => !string.Equals(x.PkColumn, "id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== ETAP 1 – Analiza tabel pod migrację PK → id ===\n");
        report.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
        report.AppendLine("## 1) Tabele z FK wychodzącymi (POMINIĘTE)\n");
        report.AppendLine(string.Join(", ", tablesWithFkOut.OrderBy(x => x)));
        report.AppendLine("\n## 2) Tabele referencjonowane przez inne (POMINIĘTE)\n");
        report.AppendLine(string.Join(", ", tablesWithFkIn.OrderBy(x => x)));
        report.AppendLine("\n## 3) Tabele spełniające warunki ETAPU 1\n");
        report.AppendLine("| Tabela | PK (obecna) | Typ | AUTO_INCREMENT |");
        report.AppendLine("|--------|-------------|-----|----------------|");
        foreach (var t in stage1)
        {
            report.AppendLine($"| {t.Table} | {t.PkColumn} | {t.FullType} | {(t.AutoIncrement ? "TAK" : "NIE")} |");
        }
        report.AppendLine($"\n**Razem: {stage1.Count} tabel**\n");

        var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        var outputPath = Path.Combine(baseDir, "..", "docs", "ETAP1_ANALIZA_RAPORT.md");
        outputPath = Path.GetFullPath(outputPath);
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(outputPath, report.ToString());

        Console.WriteLine(report.ToString());
        Console.WriteLine($"\n✓ Raport zapisany: {outputPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Błąd: {ex.Message}");
    }
}

static async Task RunEtap3Analysis(string connectionString)
{
    try
    {
        using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();
        var schema = "locbd";

        Console.WriteLine("=== ETAP 3 – Analiza KLASA C (rdzeń/rodzice) ===\n");

        var sql = @"SELECT rc.REFERENCED_TABLE_NAME AS rodzic, rc.TABLE_NAME AS dziecko, rc.CONSTRAINT_NAME,
            kcu.COLUMN_NAME AS kolumna_fk, kcu.REFERENCED_COLUMN_NAME AS ref_col,
            rc.DELETE_RULE, rc.UPDATE_RULE
            FROM information_schema.REFERENTIAL_CONSTRAINTS rc
            JOIN information_schema.KEY_COLUMN_USAGE kcu ON rc.CONSTRAINT_SCHEMA=kcu.CONSTRAINT_SCHEMA 
                AND rc.TABLE_NAME=kcu.TABLE_NAME AND rc.CONSTRAINT_NAME=kcu.CONSTRAINT_NAME
            WHERE rc.CONSTRAINT_SCHEMA=@s
            ORDER BY rc.REFERENCED_TABLE_NAME, rc.TABLE_NAME";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@s", schema);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Rodzic | Dziecko | CONSTRAINT | kolumna_fk | ref_col | DELETE | UPDATE");
        sb.AppendLine(new string('-', 90));
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                sb.AppendLine($"{r.GetString(0),-12} | {r.GetString(1),-25} | {r.GetString(2),-35} | {r.GetString(3),-12} | {r.GetString(4),-12} | {r.GetString(5),-8} | {r.GetString(6)}");
            }
        }
        Console.WriteLine(sb.ToString());

        var outPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "docs", "ETAP3_ANALIZA_WYNIK.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        await File.WriteAllTextAsync(outPath, sb.ToString());
        Console.WriteLine($"\n✓ Zapisano: {outPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Błąd: {ex.Message}");
    }
}

static async Task RunEtap2Migration(string connectionString)
{
    try
    {
        using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();
        var schema = "locbd";

        Console.WriteLine("=== ETAP 2 – Migracja PK → id (KLASA B) ===\n");

        // 1) Znajdź tabelę pozycji ofert (apozycjeoferty lub ofertypozycje)
        var offerPosTable = await GetTableNameAsync(conn, schema, "apozycjeoferty", "ofert typozycje");
        if (offerPosTable == null)
        {
            Console.WriteLine("✗ Nie znaleziono tabeli pozycji ofert (apozycjeoferty / ofert typozycje)");
            return;
        }
        Console.WriteLine($"✓ Tabela pozycji ofert: {offerPosTable}");

        // 2) Migracja ofert typozycje / apozycjeoferty
        var pkCol = await GetPkColumnAsync(conn, schema, offerPosTable);
        if (pkCol != null && pkCol != "id")
        {
            var fks = await GetOutgoingFksAsync(conn, schema, offerPosTable);
            var cntBefore = await GetRowCountAsync(conn, offerPosTable);
            Console.WriteLine($"\n--- {offerPosTable}: {pkCol} -> id (cnt={cntBefore}) ---");

            using var trans = await conn.BeginTransactionAsync();
            try
            {
                foreach (var fk in fks)
                {
                    await ExecuteNonQueryAsync(conn, trans, $"ALTER TABLE `{offerPosTable}` DROP FOREIGN KEY `{fk.ConstraintName}`");
                    Console.WriteLine($"  DROP FK: {fk.ConstraintName}");
                }
                var refTable = fks.Count > 0 ? fks[0].ReferencedTable : "aoferty";
                var refCol = fks.Count > 0 ? fks[0].ReferencedColumn : "ID_oferta";
                var fkCol = fks.Count > 0 ? fks[0].ColumnName : "ID_oferta";
                var deleteRule = fks.Count > 0 ? fks[0].DeleteRule : "CASCADE";
                var updateRule = fks.Count > 0 ? fks[0].UpdateRule : "CASCADE";

                await ExecuteNonQueryAsync(conn, trans, $"ALTER TABLE `{offerPosTable}` CHANGE COLUMN `{pkCol}` id int(15) NOT NULL AUTO_INCREMENT");
                Console.WriteLine($"  CHANGE COLUMN: {pkCol} -> id");

                if (fks.Count > 0)
                {
                    await ExecuteNonQueryAsync(conn, trans, $"ALTER TABLE `{offerPosTable}` ADD CONSTRAINT fk_{offerPosTable.Replace(" ", "_")}_oferty FOREIGN KEY (`{fkCol}`) REFERENCES `{refTable}`(`{refCol}`) ON DELETE {deleteRule} ON UPDATE {updateRule}");
                    Console.WriteLine($"  ADD FK: {fkCol} -> {refTable}({refCol})");
                }
                await trans.CommitAsync();
                var cntAfter = await GetRowCountAsync(conn, offerPosTable);
                Console.WriteLine($"  ✓ Sukces (cnt przed={cntBefore}, po={cntAfter})");
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        else
        {
            Console.WriteLine($"\n--- {offerPosTable}: PK już = id, pomijam ---");
        }

        // 3) operator_table_permissions – tylko walidacja
        var otpCnt = await GetRowCountAsync(conn, "operator_table_permissions");
        Console.WriteLine($"\n--- operator_table_permissions: cnt={otpCnt} (PK już id, bez zmian) ---");

        // 4) pozycjefaktury
        pkCol = await GetPkColumnAsync(conn, schema, "pozycjefaktury");
        if (pkCol != null && pkCol != "id")
        {
            var fks = await GetOutgoingFksAsync(conn, schema, "pozycjefaktury");
            var cntBefore = await GetRowCountAsync(conn, "pozycjefaktury");
            Console.WriteLine($"\n--- pozycjefaktury: {pkCol} -> id (cnt={cntBefore}) ---");

            using var trans = await conn.BeginTransactionAsync();
            try
            {
                foreach (var fk in fks)
                {
                    await ExecuteNonQueryAsync(conn, trans, $"ALTER TABLE pozycjefaktury DROP FOREIGN KEY `{fk.ConstraintName}`");
                    Console.WriteLine($"  DROP FK: {fk.ConstraintName}");
                }
                await ExecuteNonQueryAsync(conn, trans, $"ALTER TABLE pozycjefaktury CHANGE COLUMN `{pkCol}` id int(15) NOT NULL AUTO_INCREMENT");
                Console.WriteLine($"  CHANGE COLUMN: {pkCol} -> id");

                foreach (var fk in fks)
                {
                    var addFk = $"ALTER TABLE pozycjefaktury ADD CONSTRAINT `{fk.ConstraintName}` FOREIGN KEY (`{fk.ColumnName}`) REFERENCES `{fk.ReferencedTable}`(`{fk.ReferencedColumn}`) ON DELETE {fk.DeleteRule} ON UPDATE {fk.UpdateRule}";
                    await ExecuteNonQueryAsync(conn, trans, addFk);
                    Console.WriteLine($"  ADD FK: {fk.ColumnName} -> {fk.ReferencedTable}({fk.ReferencedColumn})");
                }
                await trans.CommitAsync();
                var cntAfter = await GetRowCountAsync(conn, "pozycjefaktury");
                Console.WriteLine($"  ✓ Sukces (cnt przed={cntBefore}, po={cntAfter})");
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        else
        {
            Console.WriteLine($"\n--- pozycjefaktury: PK już = id, pomijam ---");
        }

        Console.WriteLine("\n✓ ETAP 2 migracja zakończona!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n✗ Błąd: {ex.Message}");
        if (ex.InnerException != null) Console.WriteLine($"   {ex.InnerException.Message}");
    }
}

static async Task<string?> GetTableNameAsync(MySqlConnection conn, string schema, params string[] names)
{
    foreach (var name in names)
    {
        using var cmd = new MySqlCommand("SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA=@s AND TABLE_NAME=@n", conn);
        cmd.Parameters.AddWithValue("@s", schema);
        cmd.Parameters.AddWithValue("@n", name);
        var r = await cmd.ExecuteScalarAsync();
        if (r != null) return r.ToString();
    }
    // Fallback: szukaj tabel zawierających "pozycje" i "oferty"
    using var cmd2 = new MySqlCommand("SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA=@s AND (TABLE_NAME LIKE '%pozycje%oferty%' OR TABLE_NAME LIKE '%oferty%pozycje%') LIMIT 1", conn);
    cmd2.Parameters.AddWithValue("@s", schema);
    var r2 = await cmd2.ExecuteScalarAsync();
    return r2?.ToString();
}

static async Task<string?> GetPkColumnAsync(MySqlConnection conn, string schema, string tableName)
{
    using var cmd = new MySqlCommand("SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=@s AND TABLE_NAME=@t AND CONSTRAINT_NAME='PRIMARY' LIMIT 1", conn);
    cmd.Parameters.AddWithValue("@s", schema);
    cmd.Parameters.AddWithValue("@t", tableName);
    var r = await cmd.ExecuteScalarAsync();
    return r?.ToString();
}

static async Task<List<(string ConstraintName, string ColumnName, string ReferencedTable, string ReferencedColumn, string DeleteRule, string UpdateRule)>> GetOutgoingFksAsync(MySqlConnection conn, string schema, string tableName)
{
    var list = new List<(string, string, string, string, string, string)>();
    var sql = @"SELECT rc.CONSTRAINT_NAME, kcu.COLUMN_NAME, rc.REFERENCED_TABLE_NAME, kcu.REFERENCED_COLUMN_NAME, rc.DELETE_RULE, rc.UPDATE_RULE
        FROM information_schema.REFERENTIAL_CONSTRAINTS rc
        JOIN information_schema.KEY_COLUMN_USAGE kcu ON rc.CONSTRAINT_SCHEMA=kcu.CONSTRAINT_SCHEMA AND rc.TABLE_NAME=kcu.TABLE_NAME AND rc.CONSTRAINT_NAME=kcu.CONSTRAINT_NAME
        WHERE rc.CONSTRAINT_SCHEMA=@s AND rc.TABLE_NAME=@t";
    using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@s", schema);
    cmd.Parameters.AddWithValue("@t", tableName);
    using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        list.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5)));
    return list;
}

static async Task<long> GetRowCountAsync(MySqlConnection conn, string tableName)
{
    using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM `{tableName}`", conn);
    return Convert.ToInt64(await cmd.ExecuteScalarAsync());
}

static async Task ExecuteNonQueryAsync(MySqlConnection conn, MySqlTransaction? trans, string sql)
{
    using var cmd = new MySqlCommand(sql, conn, trans);
    await cmd.ExecuteNonQueryAsync();
}

static async Task RunFkPkReport(string connectionString)
{
    try
    {
        using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();

        var schema = "locbd";
        using (var cmd = new MySqlCommand("SELECT DATABASE()", conn))
        {
            var db = await cmd.ExecuteScalarAsync();
            if (db != null && !string.IsNullOrEmpty(db.ToString())) schema = db.ToString()!;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== RAPORT: Klasyfikacja tabel według FK i PK ===\n");
        sb.AppendLine($"Schemat: {schema}\n");

        // 1) Statystyki FK i PK
        var sql1 = @"
            SELECT t.TABLE_NAME AS tabela,
                   COALESCE((SELECT COUNT(*) FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
                             WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.REFERENCED_TABLE_NAME = t.TABLE_NAME), 0) AS incoming_fk,
                   COALESCE((SELECT COUNT(*) FROM information_schema.REFERENTIAL_CONSTRAINTS rc 
                             WHERE rc.CONSTRAINT_SCHEMA = @schema AND rc.TABLE_NAME = t.TABLE_NAME), 0) AS outgoing_fk,
                   (SELECT GROUP_CONCAT(k.COLUMN_NAME ORDER BY k.ORDINAL_POSITION)
                    FROM information_schema.KEY_COLUMN_USAGE k
                    WHERE k.TABLE_SCHEMA = @schema AND k.TABLE_NAME = t.TABLE_NAME AND k.CONSTRAINT_NAME = 'PRIMARY') AS pk_kolumna
            FROM information_schema.TABLES t
            WHERE t.TABLE_SCHEMA = @schema AND t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_NAME";
        var cmd1 = new MySqlCommand(sql1, conn);
        cmd1.Parameters.AddWithValue("@schema", schema);

        sb.AppendLine("--- 1) Statystyki FK i PK dla każdej tabeli ---\n");
        var rows = new List<(string Tabela, int In, int Out, string? Pk)>();
        using (var r = await cmd1.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                rows.Add((r.GetString(0), r.GetInt32(1), r.GetInt32(2), r.IsDBNull(3) ? null : r.GetString(3)));
            }
        }
        foreach (var row in rows)
            sb.AppendLine($"  {row.Tabela,-35} incoming_fk={row.In,2}  outgoing_fk={row.Out,2}  pk={row.Pk ?? "(brak)"}");

        // 2) Klasy
        var izolowane = rows.Where(x => x.In == 0 && x.Out == 0).Select(x => x.Tabela).OrderBy(x => x).ToList();
        var dzieci = rows.Where(x => x.In == 0 && x.Out > 0).Select(x => x.Tabela).OrderBy(x => x).ToList();
        var rdzen = rows.Where(x => x.In > 0).Select(x => x.Tabela).OrderBy(x => x).ToList();

        sb.AppendLine("\n--- 2) A) IZOLOWANE (incoming=0, outgoing=0) ---\n");
        foreach (var t in izolowane) sb.AppendLine($"  {t}");
        sb.AppendLine($"  [{izolowane.Count} tabel]");

        sb.AppendLine("\n--- B) DZIECI/LIŚCIE (incoming=0, outgoing>0) ---\n");
        foreach (var t in dzieci) sb.AppendLine($"  {t}");
        sb.AppendLine($"  [{dzieci.Count} tabel]");

        sb.AppendLine("\n--- C) RDZEŃ/RODZICE (incoming>0) ---\n");
        foreach (var t in rdzen) sb.AppendLine($"  {t}");
        sb.AppendLine($"  [{rdzen.Count} tabel]");

        // 3) Bez PK
        var bezPk = rows.Where(x => string.IsNullOrEmpty(x.Pk)).Select(x => x.Tabela).OrderBy(x => x).ToList();
        sb.AppendLine("\n--- 3) Tabele bez PRIMARY KEY ---\n");
        foreach (var t in bezPk) sb.AppendLine($"  {t}");
        sb.AppendLine(bezPk.Count == 0 ? "  (brak)" : $"  [{bezPk.Count} tabel]");

        // 4) Composite PK
        var sql4 = @"
            SELECT TABLE_NAME, GROUP_CONCAT(COLUMN_NAME ORDER BY ORDINAL_POSITION) 
            FROM information_schema.KEY_COLUMN_USAGE 
            WHERE TABLE_SCHEMA = @schema AND CONSTRAINT_NAME = 'PRIMARY'
            GROUP BY TABLE_NAME HAVING COUNT(*) > 1 ORDER BY TABLE_NAME";
        var cmd4 = new MySqlCommand(sql4, conn);
        cmd4.Parameters.AddWithValue("@schema", schema);
        var composite = new List<string>();
        using (var r = await cmd4.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
                composite.Add($"{r.GetString(0)}: {r.GetString(1)}");
        }
        sb.AppendLine("\n--- 4) Tabele ze złożonym PRIMARY KEY (composite) ---\n");
        foreach (var c in composite) sb.AppendLine($"  {c}");
        sb.AppendLine(composite.Count == 0 ? "  (brak)" : $"  [{composite.Count} tabel]");

        // 5) Podsumowanie
        sb.AppendLine("\n--- 5) Podsumowanie ---\n");
        sb.AppendLine($"  Wszystkie tabele:     {rows.Count}");
        sb.AppendLine($"  Klasa A (izolowane):  {izolowane.Count}");
        sb.AppendLine($"  Klasa B (dzieci):    {dzieci.Count}");
        sb.AppendLine($"  Klasa C (rdzeń):     {rdzen.Count}");
        sb.AppendLine($"  Bez PK:              {bezPk.Count}");
        sb.AppendLine($"  Composite PK:        {composite.Count}");

        var outPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "docs", "RAPORT_FK_PK_KLASY.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        await File.WriteAllTextAsync(outPath, sb.ToString());

        Console.WriteLine(sb.ToString());
        Console.WriteLine($"\n✓ Raport zapisany: {outPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Błąd: {ex.Message}");
    }
}

static List<string> CompareWithCode(Dictionary<string, List<ColumnInfo>> tableStructures, MySqlConnection connection)
{
    var differences = new List<string>();
    
    // Mapowanie tabel do kolumn używanych w repozytoriach
    var repositoryColumns = new Dictionary<string, List<string>>
    {
        {
            "odbiorcy",
            new List<string> { "id", "id_firmy", "Nazwa", "Nazwisko", "Imie", "Uwagi", "Tel_1", "Tel_2", "NIP",
                "Ulica_nr", "Kod_pocztowy", "Miasto", "Kraj", "Ulica_nr_wysylka", "Kod_pocztowy_wysylka",
                "Miasto_wysylka", "Kraj_wysylka", "Email_1", "Email_2", "kod", "status", "waluta", "odbiorca_typ",
                "do_oferty", "status_vat", "regon", "adres_caly" }
        },
        {
            "zamowieniahala",
            new List<string> { "id", "id_firmy", "id_zamowienia", "data_zamowienia", "id_dostawcy",
                "dostawca", "dostawca_mail", "dostawca_waluta", "id_towaru", "nazwa_towaru_draco", "nazwa_towaru",
                "status_towaru", "jednostki_zakupu", "jednostki_sprzedazy", "cena_zakupu", "przelicznik_m_kg", "ilosc",
                "uwagi", "zaznacz_do_zamowienia", "wyslano_do_zamowienia", "dostarczono", "ilosc_w_opakowaniu",
                "stawka_vat", "operator", "nr_zam_skaner" }
        }
    };

    foreach (var tableMapping in repositoryColumns)
    {
        var tableName = tableMapping.Key;
        var expectedColumns = tableMapping.Value;

        if (!tableStructures.ContainsKey(tableName))
        {
            differences.Add($"{tableName} - tabela nie istnieje w bazie danych");
            continue;
        }

        var actualColumns = tableStructures[tableName].Select(c => c.Name).ToList();

        // Znajdź kolumny w bazie, których nie ma w kodzie
        var missingInCode = actualColumns.Except(expectedColumns, StringComparer.OrdinalIgnoreCase).ToList();
        
        // Znajdź kolumny w kodzie, których nie ma w bazie
        var missingInDb = expectedColumns.Except(actualColumns, StringComparer.OrdinalIgnoreCase).ToList();

        if (missingInCode.Any())
        {
            foreach (var col in missingInCode)
            {
                differences.Add($"{tableName}.{col} - istnieje w bazie, brakuje w kodzie");
            }
        }
        
        if (missingInDb.Any())
        {
            foreach (var col in missingInDb)
            {
                differences.Add($"{tableName}.{col} - istnieje w kodzie, brakuje w bazie");
            }
        }
    }

    return differences;
}

static string GenerateStructureReport(Dictionary<string, List<ColumnInfo>> tableStructures, Dictionary<string, string> tableModelMapping)
{
    var report = new System.Text.StringBuilder();
    report.AppendLine("=== Struktura bazy danych locbd ===\n");
    report.AppendLine($"Data wygenerowania: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

    foreach (var kvp in tableStructures.OrderBy(x => x.Key))
    {
        var tableName = kvp.Key;
        var columns = kvp.Value;
        var modelName = tableModelMapping.ContainsKey(tableName) ? tableModelMapping[tableName] : "BRAK MAPOWANIA";

        report.AppendLine($"Tabela: {tableName}");
        report.AppendLine($"Model: {modelName}");
        report.AppendLine("=".PadRight(100, '='));
        report.AppendLine($"{"Kolumna",-35} {"Typ",-25} {"Null",-8} {"Klucz",-8} {"Domyślna",-15}");
        report.AppendLine("-".PadRight(100, '-'));

        foreach (var column in columns)
        {
            var nullable = column.Nullable ? "YES" : "NO";
            var defaultValue = column.Default ?? "NULL";
            report.AppendLine($"{column.Name,-35} {column.Type,-25} {nullable,-8} {column.Key,-8} {defaultValue,-15}");
        }

        report.AppendLine();
    }

    return report.ToString();
}

class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Nullable { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Default { get; set; }
    public string? Extra { get; set; }
}
