-- Skrypt do sprawdzenia struktury tabeli operatorfirma i kluczy obcych

-- 1. Sprawdzenie struktury tabeli operatorfirma
SHOW CREATE TABLE operatorfirma;

-- 2. Sprawdzenie kluczy obcych
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'operatorfirma'
  AND REFERENCED_TABLE_NAME IS NOT NULL;

-- 3. Sprawdzenie danych w tabeli operatorfirma
SELECT * FROM operatorfirma;

-- 4. Sprawdzenie czy istnieją użytkownicy w tabeli operator
SELECT id, imie_nazwisko FROM operator;

-- 5. Sprawdzenie czy istnieją firmy w tabeli firmy
SELECT id, NAZWA FROM firmy;

-- 6. Sprawdzenie relacji użytkownik-firma
SELECT 
    of.id,
    of.id_operatora,
    o.imie_nazwisko,
    of.id_firmy,
    f.NAZWA as firma_nazwa,
    of.rola
FROM operatorfirma of
LEFT JOIN operator o ON of.id_operatora = o.id
LEFT JOIN firmy f ON of.id_firmy = f.id;
