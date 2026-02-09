using System.Text;

namespace ERP.Application.Helpers;

/// <summary>
/// Helper do budowania zapytań FULLTEXT w trybie BOOLEAN (MariaDB/MySQL).
/// Input: "jan warsz" → Output: "+jan* +warsz*"
/// </summary>
public static class FulltextSearchHelper
{
    /// <summary>
    /// Buduje zapytanie BOOLEAN dla MATCH...AGAINST.
    /// Zasady: lowercase, split po spacji, usuń puste, zostaw tylko litery i cyfry,
    /// każdy token: + na początku i * na końcu, połącz spacjami.
    /// </summary>
    /// <param name="userInput">Tekst z pola wyszukiwania, np. "jan warsz"</param>
    /// <returns>Zapytanie typu "+jan* +warsz*" lub pusty string gdy input pusty</returns>
    public static string BuildBooleanFulltext(string? userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return string.Empty;

        var tokens = userInput
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => SanitizeToken(t.Trim().ToLowerInvariant()))
            .Where(t => t.Length > 0)
            .ToList();

        if (tokens.Count == 0)
            return string.Empty;

        return string.Join(" ", tokens.Select(t => $"+{t}*"));
    }

    /// <summary>Zostawia tylko litery i cyfry – usuwa znaki niedozwolone.</summary>
    private static string SanitizeToken(string token)
    {
        var sb = new StringBuilder();
        foreach (var c in token)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }
}
