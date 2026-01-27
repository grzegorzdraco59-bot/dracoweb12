using MySqlConnector;

Console.WriteLine("Sprawdzanie tabel związanych z zamówieniami...\n");

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;";

try
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ Połączenie OK!\n");

    // Sprawdź wszystkie tabele
    var command = new MySqlCommand("SHOW TABLES", connection);
    using var reader = await command.ExecuteReaderAsync();
    
    var tables = new List<string>();
    while (await reader.ReadAsync())
    {
        tables.Add(reader.GetString(0));
    }
    
    Console.WriteLine("Tabele związane z zamówieniami:");
    var orderTables = tables.Where(t => 
        t.Contains("zamow", StringComparison.OrdinalIgnoreCase) || 
        t.Contains("zamów", StringComparison.OrdinalIgnoreCase) ||
        t.Contains("order", StringComparison.OrdinalIgnoreCase) ||
        t.Contains("hal", StringComparison.OrdinalIgnoreCase)).ToList();
    
    if (orderTables.Any())
    {
        foreach (var table in orderTables)
        {
            Console.WriteLine($"  - {table}");
        }
    }
    else
    {
        Console.WriteLine("  Nie znaleziono tabel związanych z zamówieniami.");
        Console.WriteLine("\nDostępne tabele w bazie:");
        foreach (var table in tables.OrderBy(t => t))
        {
            Console.WriteLine($"  - {table}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Błąd: {ex.Message}");
}
