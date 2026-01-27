using MySqlConnector;
using System.Text;

Console.WriteLine("Wykonywanie migracji: tworzenie systemu uprawnień operatorów do tabel...\n");

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;AllowUserVariables=true;";

try
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ Połączenie OK!\n");

    // Wczytaj skrypt SQL z pliku
    var scriptPath = Path.Combine("..", "ERP.Migrations", "Scripts", "011_CreateOperatorTablePermissions.sql");
    var fullPath = Path.GetFullPath(scriptPath);
    
    if (!File.Exists(fullPath))
    {
        Console.WriteLine($"✗ Nie znaleziono pliku: {fullPath}");
        Console.WriteLine($"Aktualny katalog: {Directory.GetCurrentDirectory()}");
        return;
    }
    
    var sqlScript = await File.ReadAllTextAsync(fullPath);

    // Podziel skrypt na pojedyncze komendy (obsługa DELIMITER)
    var commands = SplitSqlScript(sqlScript);
    Console.WriteLine($"Znaleziono {commands.Count} komend do wykonania.\n");

    // Wykonaj wszystkie komendy
    foreach (var command in commands)
    {
        if (string.IsNullOrWhiteSpace(command.Trim()))
            continue;

        try
        {
            var cmdPreview = command.Length > 50 ? command.Substring(0, 50) + "..." : command;
            Console.WriteLine($"Wykonywanie: {cmdPreview}");
            using var cmd = new MySqlCommand(command, connection);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("  ✓ Sukces\n");
        }
        catch (Exception ex)
        {
            // Ignoruj błędy jeśli procedura/tabela już istnieje
            if (ex.Message.Contains("already exists") || ex.Message.Contains("Duplicate"))
            {
                Console.WriteLine($"  ℹ Już istnieje (pomijam)\n");
            }
            else
            {
                Console.WriteLine($"  ⚠ Ostrzeżenie: {ex.Message}\n");
            }
        }
    }

    Console.WriteLine("✓ Migracja zakończona!\n");

    // Sprawdź strukturę tabeli
    Console.WriteLine("Sprawdzanie struktury tabeli operator_table_permissions:");
    Console.WriteLine("=".PadRight(80, '='));
    
    var describeCommand = new MySqlCommand("DESCRIBE operator_table_permissions", connection);
    using var reader = await describeCommand.ExecuteReaderAsync();
    
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

    // Sprawdź czy procedury zostały utworzone
    await reader.CloseAsync();
    Console.WriteLine("\nSprawdzanie utworzonych procedur:");
    var proceduresCommand = new MySqlCommand(
        "SELECT ROUTINE_NAME FROM information_schema.ROUTINES " +
        "WHERE ROUTINE_SCHEMA = 'locbd' AND ROUTINE_NAME LIKE 'sp_%Operator%' " +
        "ORDER BY ROUTINE_NAME",
        connection);
    
    using var procReader = await proceduresCommand.ExecuteReaderAsync();
    var procedures = new List<string>();
    while (await procReader.ReadAsync())
    {
        procedures.Add(procReader.GetString(0));
    }
    
    if (procedures.Any())
    {
        Console.WriteLine("✓ Utworzone procedury:");
        foreach (var proc in procedures)
        {
            Console.WriteLine($"  - {proc}");
        }
    }
    else
    {
        Console.WriteLine("⚠ Nie znaleziono procedur (mogą już istnieć)");
    }

    // Sprawdź widok
    await procReader.CloseAsync();
    Console.WriteLine("\nSprawdzanie widoku:");
    var viewCommand = new MySqlCommand(
        "SELECT TABLE_NAME FROM information_schema.VIEWS " +
        "WHERE TABLE_SCHEMA = 'locbd' AND TABLE_NAME = 'v_operator_permissions_summary'",
        connection);
    
    var viewExists = await viewCommand.ExecuteScalarAsync();
    if (viewExists != null)
    {
        Console.WriteLine("✓ Widok v_operator_permissions_summary został utworzony");
    }
    else
    {
        Console.WriteLine("⚠ Widok nie został znaleziony");
    }
}
catch (Exception ex)
{
    Console.WriteLine("✗ Błąd:");
    Console.WriteLine(ex.Message);
    if (ex.InnerException != null)
    {
        Console.WriteLine($"\nInner Exception: {ex.InnerException.Message}");
    }
    Console.WriteLine("\nStack trace:");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("\nNaciśnij Enter, aby zakończyć...");
Console.ReadLine();

static List<string> SplitSqlScript(string script)
{
    var commands = new List<string>();
    var lines = script.Split('\n');
    var currentCommand = new StringBuilder();
    var delimiter = ";";
    var inDelimiterBlock = false;

    foreach (var line in lines)
    {
        var trimmedLine = line.Trim();
        
        // Obsługa DELIMITER
        if (trimmedLine.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                delimiter = parts[1];
                inDelimiterBlock = (delimiter != ";");
                continue;
            }
        }

        // Pomiń komentarze
        if (trimmedLine.StartsWith("--") || string.IsNullOrWhiteSpace(trimmedLine))
        {
            continue;
        }

        currentCommand.AppendLine(line);

        // Sprawdź czy linia kończy się delimiterem
        if (trimmedLine.EndsWith(delimiter))
        {
            var command = currentCommand.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(command))
            {
                // Usuń delimiter z końca
                command = command.Substring(0, command.Length - delimiter.Length).Trim();
                commands.Add(command);
            }
            currentCommand.Clear();
            
            if (inDelimiterBlock && delimiter != ";")
            {
                delimiter = ";";
                inDelimiterBlock = false;
            }
        }
    }

    // Dodaj ostatnią komendę jeśli istnieje
    if (currentCommand.Length > 0)
    {
        var command = currentCommand.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(command))
        {
            commands.Add(command);
        }
    }

    return commands;
}
