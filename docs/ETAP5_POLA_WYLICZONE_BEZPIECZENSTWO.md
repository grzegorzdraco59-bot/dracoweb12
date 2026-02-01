# ETAP 5 – Zasady bezpieczeństwa: pola wyliczone (ERP)

## Wymagania

- Użytkownik **NIE edytuje ręcznie**:
  - **Pozycja:** `netto_poz`, `vat_poz`, `brutto_poz`
  - **Nagłówek:** `sum_netto`, `sum_vat`, `sum_brutto`
- Te pola są **TYLKO wyliczane** (algorytm ETAP 2 + RecalculateTotals).
- Przy **korekcie**: korekta ma własne pozycje; sumy korekty liczone identycznie (ten sam algorytm + RecalculateTotals).

## Zablokowane edycje w UI

1. **WPF / Web:** W formularzach edycji faktury i pozycji faktury:
   - Pola `NettoPoz`, `VatPoz`, `BruttoPoz` (pozycja) – **tylko odczyt** (TextBlock / kolumna IsReadOnly).
   - Pola `SumNetto`, `SumVat`, `SumBrutto` (nagłówek) – **tylko odczyt**.
2. **DataGrid pozycji:** Kolumny Netto/Vat/Brutto z `IsReadOnly="True"` (cały grid pozycji jest tylko do odczytu).
3. **API / zapis:** Przy zapisie pozycji lub nagłówka nie przyjmować wartości tych pól z klienta – zawsze wyliczać po stronie serwera i zapisywać wynik.

## Spójność księgowa

- Po każdej zmianie pozycji (dodanie/edycja/usunięcie): wywołać `RecalculateTotals(invoiceId)` i zapisać `sum_netto`, `sum_vat`, `sum_brutto` w nagłówku.
- Korekta (FVK): osobne pozycje, ten sam algorytm liczenia pozycji i ten sam mechanizm RecalculateTotals dla nagłówka korekty.
