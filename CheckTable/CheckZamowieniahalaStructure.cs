using MySqlConnector;

Console.WriteLine("Sprawdzanie struktury tabeli zamowieniahala...\n");

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;";

try
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ Połączenie OK!\n");

    // Sprawdź strukturę tabeli
    var command = new MySqlCommand("DESCRIBE zamowieniahala", connection);
    using var reader = await command.ExecuteReaderAsync();
    
    Console.WriteLine("Struktura tabeli zamowieniahala:");
    Console.WriteLine("=".PadRight(100, '='));
    Console.WriteLine($"{"Kolumna",-35} {"Typ",-25} {"Null",-8} {"Klucz",-8} {"Domyślna",-15} {"Extra",-15}");
    Console.WriteLine("-".PadRight(100, '-'));
    
    while (await reader.ReadAsync())
    {
        var field = reader.GetString(0);
        var type = reader.GetString(1);
        var nullable = reader.GetString(2);
        var key = reader.GetString(3);
        var defaultValue = reader.IsDBNull(4) ? "NULL" : reader.GetString(4);
        var extra = reader.IsDBNull(5) ? "" : reader.GetString(5);
        
        Console.WriteLine($"{field,-35} {type,-25} {nullable,-8} {key,-8} {defaultValue,-15} {extra,-15}");
    }
    
    await reader.CloseAsync();
    
    // Sprawdź przykładowe dane
    var sampleCommand = new MySqlCommand("SELECT * FROM zamowieniahala_v LIMIT 3", connection);
    using var sampleReader = await sampleCommand.ExecuteReaderAsync();
    
    if (sampleReader.HasRows)
    {
        Console.WriteLine("\nPrzykładowe rekordy (pierwsze 3):");
        Console.WriteLine("-".PadRight(100, '-'));
        
        int rowCount = 0;
        while (await sampleReader.ReadAsync() && rowCount < 3)
        {
            Console.WriteLine($"\nRekord {rowCount + 1}:");
            for (int i = 0; i < sampleReader.FieldCount; i++)
            {
                var fieldName = sampleReader.GetName(i);
                var value = sampleReader.IsDBNull(i) ? "NULL" : sampleReader.GetValue(i)?.ToString() ?? "NULL";
                Console.WriteLine($"  {fieldName}: {value}");
            }
            rowCount++;
        }
    }
    else
    {
        Console.WriteLine("\nTabela jest pusta (brak przykładowych danych).");
    }
    
    Console.WriteLine("\n" + "=".PadRight(100, '='));
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Błąd: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
