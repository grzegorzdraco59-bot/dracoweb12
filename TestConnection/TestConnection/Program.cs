using MySqlConnector;

Console.WriteLine("Wykonywanie migracji: tworzenie tabeli operator_login...\n");

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;";

try
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ Połączenie OK!\n");

    // Wczytaj skrypt SQL
    var sqlScript = @"
CREATE TABLE IF NOT EXISTS operator_login (
    id INT(15) AUTO_INCREMENT PRIMARY KEY,
    id_operatora INT(15) NOT NULL,
    login VARCHAR(100) NOT NULL UNIQUE,
    haslohash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (id_operatora) REFERENCES operator(id_operatora) ON DELETE CASCADE,
    INDEX idx_login (login),
    INDEX idx_id_operatora (id_operatora)
);";

    var command = new MySqlCommand(sqlScript, connection);
    await command.ExecuteNonQueryAsync();
    
    Console.WriteLine("✓ Tabela operator_login została utworzona pomyślnie!\n");
    
    // Sprawdź strukturę tabeli
    var describeCommand = new MySqlCommand("DESCRIBE operator_login", connection);
    using var reader = await describeCommand.ExecuteReaderAsync();
    
    Console.WriteLine("Struktura tabeli operator_login:");
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
}
catch (Exception ex)
{
    Console.WriteLine("✗ Błąd:");
    Console.WriteLine(ex.Message);
    if (ex.InnerException != null)
    {
        Console.WriteLine("\nInner Exception:");
        Console.WriteLine(ex.InnerException.Message);
    }
    Console.WriteLine("\nStack trace:");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("\nNaciśnij Enter, aby zakończyć...");
Console.ReadLine();
