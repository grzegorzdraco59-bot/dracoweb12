# Analiza schematu i backfill ERP (MariaDB) – raport

**Projekt:** aplikacja desktopowa WPF (.NET). Zmiany dotyczą **wyłącznie bazy danych** i warstwy danych. Bez zmian UI.

---

## ETAP 1 – Analiza schematu

### Mapowanie nazw tabel (projekt ↔ wymagania)

| Wymagania        | Projekt (baza)   |
|------------------|------------------|
| faktury          | **faktury**      |
| faktury_pozycje  | **pozycjefaktury** |
| oferty           | **aoferty**      |
| doc_counters     | **doc_counters** |
| company_id       | **id_firmy** (w tabeli faktury) |

### Źródło schematu

- `SyncDatabase/database_structure.txt` (wygenerowany z bazy)
- Po migracji `migrate_add_missing_erp_fields.sql` w bazie są już dodane kolumny i indeksy

### Kolumny wymagane vs istniejące

**faktury:**  
doc_type, doc_year, doc_month, doc_no, doc_full_no, source_offer_id, parent_doc_id, root_doc_id, sum_netto, sum_vat, sum_brutto – dodane przez `migrate_add_missing_erp_fields.sql` (ADD COLUMN IF NOT EXISTS).

**pozycjefaktury:**  
netto_poz, vat_poz, brutto_poz – dodane. Istniejące: ilosc, cena_netto, rabat, stawka_vat – **nie zmieniane**.

**aoferty:**  
sum_brutto – dodane. total_brutto pozostaje bez zmian.

**doc_counters:**  
company_id, doc_type, year, month, last_no – w tym projekcie tabela tworzona przez `create_doc_counters.sql` z PK (company_id, doc_type, year, month). Dla istniejących baz: `doc_counters_add_month.sql` + ewentualna korekta PK.

---

## ETAP 2 – Dodanie brakujących pól (wykonane)

Plik: **`ERP.Infrastructure/Sql/migrate_add_missing_erp_fields.sql`**

- Wszystkie wymienione wyżej kolumny dodawane przez ten skrypt (idempotentnie, IF NOT EXISTS).
- Żadna istniejąca kolumna nie jest usuwana ani zmieniana.

---

## ETAP 3 – Indeksy i unikaty (wykonane)

W pliku migracji:

- **faktury:**  
  UNIQUE(id_firmy, doc_type, doc_year, doc_month, doc_no) – nazwa `uq_faktury_doc_m`  
  INDEX(id_firmy, source_offer_id), INDEX(id_firmy, parent_doc_id), INDEX(id_firmy, root_doc_id)

---

## ETAP 4 – Założenia liczenia (rabat w %)

- **Rabat:** procent 0..100 (np. 5 = 5%). **rabat NULL = 0.**
- **stawka_vat:** NULL lub tekst (np. "23", "23%", "zw") – **NULL / "zw" = 0**, inaczej parsowanie liczby (bez %).
- Zaokrąglenia **na poziomie pozycji** (2 miejsca). Sumy w nagłówku = suma pozycji. Nie liczyć dynamicznie w UI.

### Algorytm pozycji (zgodny z C# `OfferToFpfConversionService`)

1. `netto0 = ilosc * cena_netto`
2. `netto_po_rabacie = netto0 * (1 - rabat/100)`
3. `netto_poz = ROUND(netto_po_rabacie, 2)`
4. `vat_poz = ROUND(netto_poz * stawka_vat/100, 2)`
5. `brutto_poz = netto_poz + vat_poz`

### Sumy nagłówka

- `sum_netto  = SUM(netto_poz)`
- `sum_vat    = SUM(vat_poz)`
- `sum_brutto = SUM(brutto_poz)`

---

## ETAP 5 – Uzupełnienie danych historycznych (skrypty)

### Kolejność wykonania

1. **backfill_dry_run.sql** – DRY-RUN (tylko SELECT: ile rekordów, przykłady przed).
2. **backfill_pozycje_faktur.sql** – przeliczenie netto_poz, vat_poz, brutto_poz w **pozycjefaktury** (partie po 500, w transakcji).
3. **backfill_naglowki_faktur.sql** – przeliczenie sum_netto, sum_vat, sum_brutto w **faktury** z SUM(pozycjefaktury) (partie po 500, w transakcji).
4. **backfill_oferty_sum_brutto.sql** (opcjonalnie) – uzupełnienie **aoferty.sum_brutto** (np. z total_brutto).

### Pliki

| Plik | Opis |
|------|------|
| `backfill_dry_run.sql` | DRY-RUN: COUNT i 10 rekordów „przed” (pozycje + nagłówki). |
| `backfill_pozycje_faktur.sql` | UPDATE pozycjefaktury (netto_poz, vat_poz, brutto_poz), batch 500, transakcja. |
| `backfill_naglowki_faktur.sql` | UPDATE faktury (sum_netto, sum_vat, sum_brutto) z JOIN po SUM(pozycjefaktury), batch 500, transakcja. |
| `backfill_oferty_sum_brutto.sql` | UPDATE aoferty.sum_brutto (np. z total_brutto). |

---

## ETAP 6 – Bezpieczeństwo wykonania

- **DRY-RUN:** `backfill_dry_run.sql` – tylko SELECT (ile rekordów, przykłady przed/obliczone). Nic nie zmienia.
- **Batch:** UPDATE z `LIMIT 500`; powtarzać do `ROW_COUNT() = 0`.
- **Transakcje:** każda partia w `START TRANSACTION` … `COMMIT`.
- **Nic nie usuwane:** tylko UPDATE pól wyliczanych (netto_poz, vat_poz, brutto_poz, sum_netto, sum_vat, sum_brutto).

### Raport po wykonaniu

- Liczbę uzupełnionych rekordów widać po każdym uruchomieniu partii (`ROW_COUNT()`).
- Przykłady „przed / po” daje DRY-RUN (punkty 4 i 5 w `backfill_dry_run.sql`).

---

## Wynik końcowy – podsumowanie

### Lista dodanych kolumn (migracja)

- **faktury:** doc_type, doc_year, doc_month, doc_no, doc_full_no, source_offer_id, parent_doc_id, root_doc_id, sum_netto, sum_vat, sum_brutto  
- **pozycjefaktury:** netto_poz, vat_poz, brutto_poz  
- **aoferty:** sum_brutto  
- **doc_counters:** month (jeśli brak)

### Kompletne ALTER TABLE

W pliku: **`ERP.Infrastructure/Sql/migrate_add_missing_erp_fields.sql`**

### Skrypty UPDATE (backfill)

- **`backfill_pozycje_faktur.sql`** – pozycje (algorytm z rabatem w %).
- **`backfill_naglowki_faktur.sql`** – nagłówki faktur.
- **`backfill_oferty_sum_brutto.sql`** – oferty (opcjonalnie).

### Raport: ile rekordów uzupełniono

- Uruchomić **backfill_dry_run.sql** przed backfillem – dostaniesz liczbę rekordów do uzupełnienia.
- Po backfillu: powtarzać partie do `ROW_COUNT() = 0`; łączna liczba zmienionych = suma ROW_COUNT() z każdej partii.

### Przykłady 10 rekordów „przed / po”

- **Przed:** `backfill_dry_run.sql` punkty 4 i 5 – 10 pozycji i 10 nagłówków z wartościami „przed” oraz obliczonymi (netto_obliczone, vat_obliczone, sum_netto_obliczone itd.).
- **Po:** w `backfill_dry_run.sql` (punkty 6–7, zakomentowane) – po wykonaniu backfillu odkomentować i uruchomić, aby zobaczyć 10 pozycji i 10 nagłówków z uzupełnionymi polami.

### Potwierdzenie: rabat liczony jako procent

- W C#: `nettoPoRabacie = netto0 * (1m - rabatPercent / 100m)`.
- W SQL: `(1 - IFNULL(rabat,0)/100)`.
- Rabat 5% → mnożnik 0,95. **Rabat jest traktowany jako procent (0..100).**
