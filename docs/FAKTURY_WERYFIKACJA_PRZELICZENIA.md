# Weryfikacja okna Faktury – przeliczenia w DB/logice, UI tylko wyświetla

## WYNIK

- **Tabela nagłówka:** `faktury`
- **Tabela pozycji (<FAKTURY_POZYCJE>):** `pozycjefaktury`
- Kwoty liczymy po stronie DB/logiki; UI tylko pokazuje gotowe wartości.

---

## ETAP 1 – Wykryta nazwa tabeli pozycji

- **Nagłówek:** `faktury` (potwierdzone w repozytoriach i SQL).
- **Pozycje:** `pozycjefaktury` – używana w `InvoicePositionRepository`, `InvoiceTotalsService`, `backfill_pozycje_faktur.sql`, `migrate_add_missing_erp_fields.sql`.

Weryfikacja w bazie:
```sql
SHOW TABLES;
SHOW TABLES LIKE '%fakt%poz%';
SHOW TABLES LIKE '%poz%fakt%';
```
Skrypt: `ERP.Infrastructure/Sql/verify_faktury_schema.sql`.

---

## ETAP 2 – Kolumny (nagłówek i pozycje)

**Pozycje (`pozycjefaktury`):**
- Wejściowe (edytowalne): `ilosc`, `cena_netto`, `rabat`, `stawka_vat`
- Wyliczane: `netto_poz`, `vat_poz`, `brutto_poz` (w kodzie używane są `vat_poz` i `brutto_poz`; w specyfikacji czasem „vat_pozycji” – w DB kolumna to `vat_poz`)

**Nagłówek (`faktury`):**
- Wyliczane z pozycji: `sum_netto`, `sum_vat`, `sum_brutto`
- Opcjonalnie: `sum_zaliczek_brutto`, `do_zaplaty_brutto` (fix_faktury_do_zaplaty_brutto.sql)

**ALTER TABLE (gdy brak kolumn):**
- `pozycjefaktury_add_amounts.sql`:
```sql
ALTER TABLE pozycjefaktury
  ADD COLUMN netto_poz  DECIMAL(18,2) NULL,
  ADD COLUMN vat_poz    DECIMAL(18,2) NULL,
  ADD COLUMN brutto_poz DECIMAL(18,2) NULL;
```
- `migrate_add_missing_erp_fields.sql` – ADD COLUMN IF NOT EXISTS dla `netto_poz`, `vat_poz`, `brutto_poz`
- Nagłówek: `faktury_add_sum_fields.sql`, `fix_faktury_do_zaplaty_brutto.sql`

---

## ETAP 3–4 – SQL backfill (pozycje + nagłówki)

**Backfill w jednej transakcji (wszystkie rekordy):**  
`ERP.Infrastructure/Sql/backfill_faktury_pozycje_i_naglowki.sql`

- **Pozycje:** UPDATE `pozycjefaktury` SET `netto_poz`, `vat_poz`, `brutto_poz` według wzorów (zaokrąglenie na pozycji; `stawka_vat` może być tekstem np. „23%” – parsowanie w SQL).
- **Nagłówki:** UPDATE `faktury` z JOIN na SUM(netto_poz), SUM(vat_poz), SUM(brutto_poz) po `id_faktury`.

Wzory pozycji:
- `netto_poz = ROUND( ilosc * cena_netto * (1 - IFNULL(rabat,0)/100), 2)`
- `vat_poz   = ROUND( netto_poz * (stawka_vat_num/100), 2)` (stawka_vat_num z parsowania tekstu)
- `brutto_poz = netto_poz + vat_poz`

Istniejące skrypty partiami: `backfill_pozycje_faktur.sql`, `backfill_naglowki_faktur.sql`.

---

## ETAP 5 – WPF okno „Faktury”

**Miejsce:** `ERP.UI.WPF/Views/FakturyView.xaml`, `InvoicesViewModel.cs`.

- **Lista faktur:** DataGrid łączy się z `FilteredInvoices`; kolumna „Brutto” wiązana z **SumBrutto** (suma z pozycji), bez liczenia w XAML.
- **Pozycje faktury:** DataGrid `Positions` – tylko odczyt; kolumny Netto/VAT/Brutto wiązane z `NettoPoz`, `VatPoz`, `BruttoPoz` (StringFormat `{0:N2}`), **bez konwerterów liczących**.
- **Dodawanie/edycja/usuwanie pozycji:** przyciski są wyłączone (IsEnabled="False"); funkcjonalność w przygotowaniu. Po wdrożeniu należy po każdej zmianie pozycji wywołać:
  1. `IInvoiceTotalsService.RecalculateInvoicePositionsAndTotalsAsync(invoiceId)` – przelicza w DB `netto_poz`, `vat_poz`, `brutto_poz` dla wszystkich pozycji faktury, potem `sum_netto`, `sum_vat`, `sum_brutto` w nagłówku.
  2. Odświeżenie listy pozycji i nagłówka (reload z repozytorium) – bez liczenia w XAML.

**Zmiany w plikach:**
- `FakturyView.xaml` – kolumna Brutto: Binding z `KwotaBrutto` na **SumBrutto** (StringFormat, TargetNullValue).
- Brak zmian DataContext ani konstruktorów ViewModeli; brak nowych property ani logiki poza wiązaniem do SumBrutto.

---

## Serwis przeliczeń

- **IInvoiceTotalsService** / **InvoiceTotalsService**:
  - `RecalculateInvoicePositionsAndTotalsAsync(invoiceId)` – **nowe:** przelicza pozycje (netto_poz, vat_poz, brutto_poz) w DB, potem sumy nagłówka (sum_netto, sum_vat, sum_brutto).
  - `RecalculateTotalsAsync(invoiceId)` – tylko sumy nagłówka z SUM(pozycji).
  - `RecalculateFinalInvoicePaymentsAsync(fvId)` – dla FV: sum_zaliczek_brutto, do_zaplaty_brutto.

Po wdrożeniu edycji pozycji w WPF: po zapisie/usuwięciu pozycji wywołać `RecalculateInvoicePositionsAndTotalsAsync`, następnie odświeżyć dane w UI.

---

## ETAP 6 – Testy (do wykonania ręcznie)

1. Otwórz fakturę z pozycjami – sprawdź, że netto_poz, vat_poz, brutto_poz są niezerowe i spójne (netto + vat = brutto).
2. Po backfillu: zmiana ilosc/cena_netto/rabat/stawka_vat (gdy będzie edycja) → przeliczenie pozycji i sum nagłówka przez serwis.
3. Porównanie z DB: `faktury.sum_netto/sum_vat/sum_brutto` = SUM(netto_poz), SUM(vat_poz), SUM(brutto_poz) z `pozycjefaktury` dla danego `id_faktury`.

---

## Lista zmienionych/dodanych plików

| Plik | Opis |
|------|------|
| `ERP.Infrastructure/Sql/verify_faktury_schema.sql` | **Nowy** – SHOW TABLES, SHOW COLUMNS dla faktury i pozycjefaktury |
| `ERP.Infrastructure/Sql/backfill_faktury_pozycje_i_naglowki.sql` | **Nowy** – backfill pozycji + nagłówków w jednej transakcji |
| `ERP.Application/Services/IInvoiceTotalsService.cs` | **Zmiana** – dodane `RecalculateInvoicePositionsAndTotalsAsync` |
| `ERP.Infrastructure/Services/InvoiceTotalsService.cs` | **Zmiana** – implementacja przeliczania pozycji + sum nagłówka |
| `ERP.UI.WPF/Views/FakturyView.xaml` | **Zmiana** – kolumna Brutto → Binding na SumBrutto |
| `docs/FAKTURY_WERYFIKACJA_PRZELICZENIA.md` | **Nowy** – ten raport |

---

## Potwierdzenie

- Okno „Faktury” wyświetla kwoty z bazy (SumBrutto w liście; NettoPoz, VatPoz, BruttoPoz w pozycjach).
- Przeliczenia wykonywane w DB/logice (backfill SQL, InvoiceTotalsService); UI nie liczy kwot w XAML (brak konwerterów liczących).
