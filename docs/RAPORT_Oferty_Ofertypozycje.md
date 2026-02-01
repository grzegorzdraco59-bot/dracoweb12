# Raport: dopasowanie projektu WPF do tabel oferty, ofertypozycje

**Projekt:** aplikacja desktopowa WPF (.NET). Zmiany tylko w warstwie DB i dostępu do danych. UI nie zmieniane (poza użyciem SumBrutto w drzewku).

---

## 1. Lista zmienionych plików

### Repozytoria (Infrastructure)
- **OfferRepository.cs** – tabela `aoferty` → `oferty`, kolumna `ID_oferta` → `id`, dodane `sum_brutto` (SELECT, INSERT, UPDATE, MapToOffer).
- **OfferPositionRepository.cs** – tabela `apozycjeoferty` → `ofertypozycje`, kolumna `ID_oferta` → `oferta_id`, dodane obliczanie i zapis `netto_poz`, `vat_poz`, `brutto_poz` (ComputePositionAmounts, ParseVatRate, AddOfferPositionParameters, UPDATE).

### Serwisy (Application + Infrastructure)
- **IOfferTotalsService.cs** (nowy) – interfejs przeliczania `oferty.sum_brutto` z pozycji.
- **OfferTotalsService.cs** (nowy) – implementacja: SUM(brutto_poz) z ofertypozycje → UPDATE oferty.sum_brutto.
- **OfferService.cs** – wstrzyknięcie IOfferTotalsService, wywołanie RecalculateSumBruttoAsync po AddPositionAsync, UpdatePositionAsync, DeletePositionAsync.
- **IOfferToFpfConversionService.cs** – komentarze: aoferty → oferty, ID_oferta → oferty.id.
- **OfferToFpfConversionService.cs** – komentarz: oferty + ofertypozycje.

### Inne (Infrastructure)
- **OrderPositionMainRepository.cs** – JOIN: `apozycjeoferty` → `ofertypozycje`, `ID_oferta` → `oferta_id`.

### Encje i DTO (Domain, Application)
- **Offer.cs** – właściwość `SumBrutto`, metoda `UpdateSumBrutto`.
- **OfferDto.cs** – właściwość `SumBrutto`.

### UI (WPF) – tylko dane
- **App.xaml.cs** – rejestracja `IOfferTotalsService` → `OfferTotalsService`.
- **OffersViewModel.cs** – drzewko: kwota brutto oferty = `SumBrutto ?? TotalBrutto`, MapToDto: `SumBrutto = offer.SumBrutto`.

### SQL (Infrastructure/Sql)
- **migrate_add_missing_erp_fields.sql** – `aoferty` → `oferty` (ALTER TABLE oferty ADD sum_brutto).
- **backfill_dry_run.sql** – `aoferty` → `oferty`.
- **backfill_oferty_sum_brutto.sql** – `aoferty` → `oferty`, komentarze zaktualizowane.
- **oferty_ofertypozycje_add_columns.sql** (nowy) – ADD sum_brutto do oferty, ADD netto_poz, vat_poz, brutto_poz do ofertypozycje.
- **backfill_ofertypozycje_oferty.sql** (nowy) – UPDATE pozycji (netto_poz, vat_poz, brutto_poz), UPDATE nagłówków (sum_brutto).
- **rename_aoferty_to_oferty.sql** (nowy) – opcjonalnie: RENAME TABLE + CHANGE COLUMN (id, oferta_id).

---

## 2. ALTER TABLE (dodane pola)

- **oferty:** `sum_brutto DECIMAL(18,2) NULL DEFAULT 0.00` – plik: `oferty_ofertypozycje_add_columns.sql` lub `migrate_add_missing_erp_fields.sql`.
- **ofertypozycje:** `netto_poz`, `vat_poz`, `brutto_poz` DECIMAL(18,2) NULL – plik: `oferty_ofertypozycje_add_columns.sql`.

---

## 3. Skrypty UPDATE (przeliczenia)

- **backfill_ofertypozycje_oferty.sql** – przeliczenie pozycji (netto_poz, vat_poz, brutto_poz) oraz nagłówków (sum_brutto). Partie po 500, w transakcjach.
- **backfill_oferty_sum_brutto.sql** – alternatywa: oferty.sum_brutto = total_brutto.

---

## 4. Algorytm liczenia pozycji oferty (ETAP 3)

- Rabat w procentach (0..100), rabat NULL = 0.
- netto0 = ilosc * cena_netto  
- netto_po_rabacie = netto0 * (1 - rabat/100)  
- netto_poz = ROUND(netto_po_rabacie, 2)  
- vat_poz = ROUND(netto_poz * stawka_vat/100, 2) (stawka_vat NULL/zw = 0)  
- brutto_poz = netto_poz + vat_poz  

Zapis w repozytorium: przy INSERT/UPDATE pozycji oferty wartości netto_poz, vat_poz, brutto_poz są liczone w `OfferPositionRepository.ComputePositionAmounts` i zapisywane do `ofertypozycje`.

---

## 5. Suma brutto oferty (ETAP 4)

- **oferty.sum_brutto = SUM(ofertypozycje.brutto_poz)** WHERE oferta_id = oferty.id.
- Serwis **IOfferTotalsService / OfferTotalsService** po każdej zmianie pozycji (dodanie, edycja, usunięcie) wywołuje RecalculateSumBruttoAsync(offerId).
- W drzewku dokumentów wyświetlana jest kwota: **SumBrutto ?? TotalBrutto** (preferowane SumBrutto z nagłówka).

---

## 6. Uzupełnienie danych historycznych (ETAP 5)

- Uruchomić **backfill_ofertypozycje_oferty.sql**: najpierw UPDATE pozycji (partie 500), potem UPDATE nagłówków (partie 500), do ROW_COUNT() = 0.
- Transakcje: każda partia w START TRANSACTION … COMMIT.

---

## 7. Spójność z FPF (ETAP 6)

- FPF i faktury nie są zmieniane (faktury nadal używają id_oferty).
- Jeśli oferta ma powiązaną FPF (faktury.doc_type='FPF' AND faktury.id_oferty = oferty.id), docelowo oferty.sum_brutto może być zgodne z FPF.sum_brutto (np. po backfillu oferty i FPF). Obecna logika: sum_brutto oferty = SUM(brutto_poz) z ofertypozycje; po „Kopiuj do FPF” FPF ma własne sum_brutto z pozycjefaktury.

---

## 8. Potwierdzenie

- **oferty.sum_brutto** jest wyliczane jako SUM(ofertypozycje.brutto_poz) i zapisywane w nagłówku oferty (OfferTotalsService).
- Po dodaniu/edycji/usunięciu pozycji oferty sum_brutto jest przeliczane automatycznie.
- Pozycje oferty zapisują netto_poz, vat_poz, brutto_poz według algorytmu z rabatem w % i zaokrągleniami na poziomie pozycji.
- Projekt WPF używa wyłącznie nazw tabel: **oferty**, **ofertypozycje** oraz kolumn **id** (oferty), **oferta_id** (ofertypozycje). W bazie muszą być te nazwy (w razie potrzeby: rename_aoferty_to_oferty.sql).
