# ETAP 1 – Weryfikacja schematu oferty / ofertypozycje

Wykonaj w bazie MariaDB/MySQL (bez zgadywania):

```sql
SHOW COLUMNS FROM oferty;
SHOW COLUMNS FROM ofertypozycje;
```

## Oczekiwane kolumny

### ofertypozycje
- **ilosc** – ilość (po zmianie nazwy z Sztuki)
- **Cena** – cena netto (jeśli jest **cena_netto** – używana w backfillie)
- **Rabat** – rabat % (0..100)
- **stawka_vat** – stawka VAT % (VARCHAR, np. "23,00")
- **oferta_id** – FK do oferty
- **netto_poz**, **vat_poz**, **brutto_poz** – pola wyliczane (dodane przez `oferty_ofertypozycje_add_columns.sql`)

### oferty
- **id** – PK
- **sum_brutto** – suma brutto z pozycji (SUM(brutto_poz))

## ALTER TABLE (jeśli brakuje pól wyliczanych)

```sql
-- oferty
ALTER TABLE oferty ADD COLUMN IF NOT EXISTS sum_brutto DECIMAL(18,2) NULL DEFAULT 0.00;

-- ofertypozycje
ALTER TABLE ofertypozycje ADD COLUMN IF NOT EXISTS netto_poz  DECIMAL(18,2) NULL;
ALTER TABLE ofertypozycje ADD COLUMN IF NOT EXISTS vat_poz    DECIMAL(18,2) NULL;
ALTER TABLE ofertypozycje ADD COLUMN IF NOT EXISTS brutto_poz DECIMAL(18,2) NULL;
```

Pełny skrypt: **ERP.Infrastructure/Sql/oferty_ofertypozycje_add_columns.sql**

## Mapowanie (dla backfilla / logiki)

| Znaczenie    | Kolumna w ofertypozycje |
|-------------|--------------------------|
| Ilość       | ilosc                    |
| Cena netto  | Cena                     |
| Rabat %     | Rabat                    |
| Stawka VAT %| stawka_vat (VARCHAR)     |
| FK oferty   | oferta_id                |
