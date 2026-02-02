using System.Text.RegularExpressions;

namespace ERP.Application.Helpers;

/// <summary>
/// Numer oferty: OF/YYYY/MM/DD-NNNN (z data_oferty + nr_oferty).
/// Nie budować numeru z tekstu statusu ani innych pól – tylko deterministycznie z daty i numeru.
/// </summary>
public static class OfferNumberHelper
{
    private const int MaxFileNameLength = 120;

    /// <summary>Numer oferty do wyświetlania i nagłówka PDF: OF/yyyy/MM/dd-N.</summary>
    public static string BuildOfferNo(DateTime dataOferty, int nrOferty)
        => $"OF/{dataOferty:yyyy/MM/dd}-{nrOferty}";

    /// <summary>Domyślna nazwa pliku PDF w SaveFileDialog: OF_yyyy-MM-dd-N.pdf (bez slashy w nazwie pliku).</summary>
    public static string BuildOfferFileName(DateTime dataOferty, int nrOferty)
        => $"OF_{dataOferty:yyyy-MM-dd}-{nrOferty}.pdf";

    /// <summary>Domyślna nazwa pliku PDF z klientem: OF_yyyy-MM-dd-N_Klient.pdf. Nazwa klienta jest sanityzowana.</summary>
    public static string BuildOfferFileNameWithClient(DateTime dataOferty, int nrOferty, string? customerName)
    {
        var baseName = $"OF_{dataOferty:yyyy-MM-dd}-{nrOferty}";
        var safe = SanitizeFileName(customerName);
        if (string.IsNullOrWhiteSpace(safe))
            return baseName + ".pdf";
        var full = $"{baseName}_{safe}.pdf";
        return full.Length > MaxFileNameLength ? full[..(MaxFileNameLength - 4)] + ".pdf" : full;
    }

    /// <summary>Usuwa znaki niedozwolone w Windows (\ / : * ? " &lt; &gt; |), zamienia spacje na '_', usuwa polskie znaki (ASCII-safe), przycina do MaxFileNameLength.</summary>
    public static string SanitizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var s = value.Trim();
        s = Regex.Replace(s, @"[\x00-\x1F\\/:*?""<>|]", "");
        s = s.Replace(' ', '_');
        s = RemovePolishChars(s);
        s = Regex.Replace(s, @"_+", "_").Trim('_');
        return s.Length > MaxFileNameLength ? s[..MaxFileNameLength] : s;
    }

    private static string RemovePolishChars(string s)
    {
        var map = new Dictionary<char, char>
        {
            {'ą','a'},{'ć','c'},{'ę','e'},{'ł','l'},{'ń','n'},{'ó','o'},{'ś','s'},{'ź','z'},{'ż','z'},
            {'Ą','A'},{'Ć','C'},{'Ę','E'},{'Ł','L'},{'Ń','N'},{'Ó','O'},{'Ś','S'},{'Ź','Z'},{'Ż','Z'}
        };
        var arr = s.ToCharArray();
        for (var i = 0; i < arr.Length; i++)
            if (map.TryGetValue(arr[i], out var c))
                arr[i] = c;
        return new string(arr);
    }
}
