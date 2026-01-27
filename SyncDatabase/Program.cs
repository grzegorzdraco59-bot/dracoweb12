using MySqlConnector;

Console.WriteLine("=== Synchronizacja struktury bazy danych locbd z projektem ===\n");

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;";

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

static List<string> CompareWithCode(Dictionary<string, List<ColumnInfo>> tableStructures, MySqlConnection connection)
{
    var differences = new List<string>();
    
    // Mapowanie tabel do kolumn używanych w repozytoriach
    var repositoryColumns = new Dictionary<string, List<string>>
    {
        {
            "odbiorcy",
            new List<string> { "ID_odbiorcy", "id_firmy", "Nazwa", "Nazwisko", "Imie", "Uwagi", "Tel_1", "Tel_2", "NIP",
                "Ulica_nr", "Kod_pocztowy", "Miasto", "Kraj", "Ulica_nr_wysylka", "Kod_pocztowy_wysylka",
                "Miasto_wysylka", "Kraj_wysylka", "Email_1", "Email_2", "kod", "status", "waluta", "odbiorca_typ",
                "do_oferty", "status_vat", "regon", "adres_caly" }
        },
        {
            "zamowieniahala",
            new List<string> { "id_zamowienia_hala", "id_firmy", "id_zamowienia", "data_zamowienia", "id_dostawcy",
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
