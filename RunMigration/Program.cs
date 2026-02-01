using MySqlConnector;
using System.Text;

var connectionString = "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=dracogk0909;SslMode=None;AllowUserVariables=true;";

try
{
    using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ Połączenie OK\n");

    var scriptPath = Path.Combine("..", "ERP.Infrastructure", "Sql", "oferty_add_id_pk.sql");
    var fullPath = Path.GetFullPath(scriptPath);
    if (!File.Exists(fullPath))
    {
        Console.WriteLine($"✗ Plik migracji nie znaleziony: {fullPath}");
        Console.WriteLine("\nNaciśnij Enter...");
        Console.ReadLine();
        return;
    }

    var sqlScript = await File.ReadAllTextAsync(fullPath);
    var commands = SplitSqlScript(sqlScript);
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
}
catch (Exception ex)
{
    Console.WriteLine("✗ Błąd:");
    Console.WriteLine(ex.Message);
    if (ex.InnerException != null)
        Console.WriteLine($"\nInner: {ex.InnerException.Message}");
    Console.WriteLine("\nStack trace:");
    Console.WriteLine(ex.StackTrace);
}

Console.WriteLine("Naciśnij Enter, aby zakończyć...");
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

        if (trimmedLine.StartsWith("--") || string.IsNullOrWhiteSpace(trimmedLine))
            continue;

        currentCommand.AppendLine(line);

        if (trimmedLine.EndsWith(delimiter))
        {
            var command = currentCommand.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(command))
            {
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

    if (currentCommand.Length > 0)
    {
        var command = currentCommand.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(command))
            commands.Add(command);
    }

    return commands;
}
