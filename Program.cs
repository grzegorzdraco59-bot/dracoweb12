// Program.cs jest używany do testów - zobacz TestDatabaseConnection.cs

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;";

try
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("Połączenie OK!\n");

    // Pobierz strukturę tabeli
    var command = new MySqlCommand("DESCRIBE Odbiorcy", connection);
    using var reader = await command.ExecuteReaderAsync();
    
    Console.WriteLine("Struktura tabeli Odbiorcy:");
    Console.WriteLine("=".PadRight(80, '='));
    Console.WriteLine($"{"Kolumna",-25} {"Typ",-25} {"Null",-8} {"Klucz",-8} {"Domyślna",-15}");
    Console.WriteLine("-".PadRight(80, '-'));
    
    while (await reader.ReadAsync())
    {
        var field = reader.GetString(0);
        var type = reader.GetString(1);
        var nullable = reader.GetString(2);
        var key = reader.GetString(3);
        var defaultValue = reader.IsDBNull(4) ? "NULL" : reader.GetString(4);
        
        Console.WriteLine($"{field,-25} {type,-25} {nullable,-8} {key,-8} {defaultValue,-15}");
    }
    
    Console.WriteLine("\n" + "=".PadRight(80, '='));
    
    // Pobierz przykładowe dane (jeśli istnieją)
    await reader.CloseAsync();
    var sampleCommand = new MySqlCommand("SELECT * FROM Odbiorcy LIMIT 1", connection);
    using var sampleReader = await sampleCommand.ExecuteReaderAsync();
    
    if (await sampleReader.ReadAsync())
    {
        Console.WriteLine("\nPrzykładowy rekord:");
        Console.WriteLine("-".PadRight(80, '-'));
        for (int i = 0; i < sampleReader.FieldCount; i++)
        {
            var fieldName = sampleReader.GetName(i);
            var value = sampleReader.IsDBNull(i) ? "NULL" : sampleReader.GetValue(i)?.ToString() ?? "NULL";
            Console.WriteLine($"{fieldName}: {value}");
        }
    }
    else
    {
        Console.WriteLine("\nTabela jest pusta (brak przykładowych danych).");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Błąd:");
    Console.WriteLine(ex.Message);
    Console.WriteLine("\nStack trace:");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("\nNaciśnij Enter, aby zakończyć...");
Console.ReadLine();
