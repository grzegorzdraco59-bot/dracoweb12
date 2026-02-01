# Schemat tabel oferty i ofertypozycje – mapowanie kolumn

Ustalone na podstawie: backupu bazy (CREATE TABLE), `OfferPositionRepository`, `OfferRepository`, encji `Offer`/`OfferPosition`.

---

## oferty

| Kolumna w DB   | Typ / uwagi        | Mapowanie w aplikacji |
|----------------|--------------------|------------------------|
| id             | INT(15) PK         | Offer.Id               |
| id_firmy       | INT(15)            | CompanyId              |
| Data_oferty    | INT(10)            | OfferDate (Unix)       |
| Nr_oferty      | INT(4)             | OfferNumber            |
| total_brutto   | DECIMAL(15,2)      | TotalBrutto (legacy)   |
| **sum_brutto** | DECIMAL(18,2)      | **SumBrutto** – suma z pozycji `SUM(ofertypozycje.brutto_poz)` |

Suma brutto oferty w UI i logice: **oferty.sum_brutto** (policzona w DB/serwisie, nie w XAML).

---

## ofertypozycje

### Mapowanie kolumn wejściowych (bez zgadywania)

| Znaczenie          | Kolumna w DB (faktyczna) | Typ w DB        | Mapowanie w aplikacji |
|--------------------|---------------------------|-----------------|------------------------|
| **Ilość**          | **ilosc**                 | DECIMAL(18,3)   | Quantity               |
| **Cena netto**     | **Cena**                  | DECIMAL(15,2)   | Price                  |
| **Rabat %**        | **Rabat**                 | DECIMAL(15,2)   | Discount               |
| **Stawka VAT %**   | **stawka_vat**            | VARCHAR(10)     | VatRate (np. "23,00")  |
| **Powiązanie z ofertą** | **oferta_id**      | INT(15)         | OfferId                |

### Kolumny legacy (pozostawione, nie usuwane)

- Cena_po_rabacie, Cena_po_rabacie_i_sztukach  
- vat (kwota VAT), cena_brutto  

### Kolumny wyliczane (pozycja)

- **netto_poz** = ROUND( ilość × cena_netto × (1 − rabat/100), 2 )  
- **vat_poz**   = ROUND( netto_poz × (stawka_vat/100), 2 )  
- **brutto_poz** = netto_poz + vat_poz  

Źródło: ilość = ilosc (kolumna), cena_netto = Cena, rabat = Rabat; stawka_vat z VARCHAR parsowana do liczby (przecinek → kropka, bez %).  
NULL rabat / NULL stawka_vat traktowane jako 0.

### Kolumny standardowe (docelowe, opcjonalne w DB)

Jeśli dodane w migracji (np. `ofertypozycje_standard_columns.sql`):

- **ilosc** DECIMAL(18,3) – kolumna ilości (dawniej Sztuki)  
- **cena_netto** DECIMAL(18,4) – backfill z Cena  
- **rabat** DECIMAL(9,2) – backfill z Rabat  
- **stawka_vat_dec** DECIMAL(9,2) – backfill z parsowania VARCHAR stawka_vat (np. "23,00" → 23.00)  

Aplikacja odczytuje/zapisuje **ilosc** (SELECT/INSERT/UPDATE); Cena, Rabat, stawka_vat (VARCHAR) – legacy. Wyświetlanie brutto pozycji: preferowane **brutto_poz** (wyliczone w DB), fallback na cena_brutto.

---

## Weryfikacja w bazie (ręcznie)

```sql
SHOW COLUMNS FROM oferty;
SHOW COLUMNS FROM ofertypozycje;
```

Po backfillzie: `sum_brutto > 0` dla ofert mających pozycje z wypełnionymi ilosc/Cena.

---

## Kolejność migracji SQL

1. **oferty_ofertypozycje_add_columns.sql** – dodaje `oferty.sum_brutto`, `ofertypozycje.netto_poz`, `vat_poz`, `brutto_poz` (wymagane przed backfillem i przed uruchomieniem WPF z nowym SELECT).
2. **ofertypozycje_standard_columns.sql** (opcjonalnie) – dodaje `cena_netto`, `rabat`, `stawka_vat_dec` i backfill z kolumn legacy (kolumna `ilosc` już w tabeli).
3. **backfill_ofertypozycje_oferty.sql** – przelicza `netto_poz`, `vat_poz`, `brutto_poz` dla wszystkich pozycji oraz `oferty.sum_brutto` z SUM(brutto_poz).
