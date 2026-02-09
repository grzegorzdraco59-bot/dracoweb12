# FULLTEXT Search – aoferty – raport

## 1. Kolumny objęte FULLTEXT

Na podstawie `SHOW COLUMNS FROM aoferty` i `database_structure.txt`:

| Kolumna | Typ |
|---------|-----|
| odbiorca_nazwa | varchar(100) |
| odbiorca_ulica | varchar(100) |
| odbiorca_kod_poczt | varchar(20) |
| odbiorca_miasto | varchar(100) |
| odbiorca_panstwo | varchar(100) |
| odbiorca_nip | varchar(20) |
| odbiorca_mail | varchar(100) |
| Waluta | varchar(5) |
| uwagi_do_oferty | varchar(800) |
| dane_dodatkowe | varchar(800) |
| operator | varchar(50) |
| uwagi_targi | varchar(1000) |
| historia | varchar(50) |
| status | varchar(20) – dodane przez faza4_krok2 |

---

## 2. ALTER TABLE (FULLTEXT INDEX)

```sql
ALTER TABLE aoferty
  ADD FULLTEXT INDEX ft_aoferty_alltext (
    odbiorca_nazwa,
    odbiorca_ulica,
    odbiorca_kod_poczt,
    odbiorca_miasto,
    odbiorca_panstwo,
    odbiorca_nip,
    odbiorca_mail,
    Waluta,
    uwagi_do_oferty,
    dane_dodatkowe,
    operator,
    uwagi_targi,
    historia,
    status
  );
```

Skrypt migracji: `ERP.Migrations/Scripts/024_AofertyFulltextIndex.sql`

---

## 3. SELECT (zapytanie wyszukiwania)

```sql
SELECT id_oferta, id_firmy, do_proformy, do_zlecenia, Data_oferty, Nr_oferty,
       odbiorca_ID_odbiorcy, odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto,
       odbiorca_panstwo, odbiorca_nip, odbiorca_mail, Waluta, Cena_calkowita, stawka_vat,
       total_vat, total_brutto, sum_netto, sum_vat, sum_brutto, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi,
       do_faktury, historia, status
FROM aoferty
WHERE id_firmy = @CompanyId
  AND (@Q IS NULL OR @Q = '' OR MATCH(
    odbiorca_nazwa, odbiorca_ulica, odbiorca_kod_poczt, odbiorca_miasto, odbiorca_panstwo,
    odbiorca_nip, odbiorca_mail, Waluta, uwagi_do_oferty, dane_dodatkowe, operator, uwagi_targi, historia, status
  ) AGAINST (@Q IN BOOLEAN MODE))
ORDER BY id_oferta DESC
LIMIT 200;
```

---

## 4. C# – helper do budowy zapytania BOOLEAN

Plik: `ERP.Application/Helpers/FulltextSearchHelper.cs`

```csharp
using System.Text;

namespace ERP.Application.Helpers;

public static class FulltextSearchHelper
{
    private static readonly char[] SpecialChars = { '+', '-', '@', '>', '<', '(', ')', '~', '*', '"', '\'' };

    /// <summary>
    /// Buduje zapytanie BOOLEAN dla MATCH...AGAINST.
    /// Input: "jan warsz pol" → Output: "+jan* +warsz* +pol*"
    /// </summary>
    public static string? BuildBooleanQuery(string? userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return null;

        var tokens = userInput
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => SanitizeToken(t.Trim().ToLowerInvariant()))
            .Where(t => t.Length > 0)
            .ToList();

        if (tokens.Count == 0)
            return null;

        return string.Join(" ", tokens.Select(t => $"+{t}*"));
    }

    private static string SanitizeToken(string token)
    {
        var sb = new StringBuilder();
        foreach (var c in token)
        {
            if (Array.IndexOf(SpecialChars, c) >= 0)
                continue;
            if (char.IsWhiteSpace(c))
                continue;
            sb.Append(c);
        }
        return sb.ToString();
    }
}
```

---

## 5. Zachowanie UI

- Debounce 300 ms – wyszukiwanie odpala się po 300 ms od ostatniego wpisania znaku
- LIMIT 200 – maksymalnie 200 wyników
- Puste pole – ładowanie wszystkich ofert (GetByCompanyIdAsync)
- Niepuste pole – FULLTEXT search (SearchByCompanyIdAsync)
