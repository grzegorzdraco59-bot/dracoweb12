-- Skrypt do sprawdzenia struktury tabel zamowienia i pozyjezamowienia
-- oraz utworzenia klucza obcego jeśli potrzeba

-- 1. Sprawdź strukturę tabeli zamowienia
SELECT '=== Struktura tabeli zamowienia ===' AS Info;
DESCRIBE zamowienia;

-- 2. Sprawdź strukturę tabeli pozycjezamowienia
SELECT '=== Struktura tabeli pozycjezamowienia ===' AS Info;
DESCRIBE pozycjezamowienia;

-- 3. Sprawdź przykładowe dane z zamowienia
SELECT '=== Przykładowe zamówienia (pierwsze 5) ===' AS Info;
SELECT * FROM zamowienia LIMIT 5;

-- 4. Sprawdź przykładowe dane z pozycjezamowienia
SELECT '=== Przykładowe pozycje zamówienia (pierwsze 10) ===' AS Info;
SELECT * FROM pozycjezamowienia LIMIT 10;

-- 5. Sprawdź czy istnieją pozycje dla istniejących zamówień
SELECT '=== Sprawdzenie dopasowania ID ===' AS Info;
SELECT 
    z.id AS zamowienie_id,
    COUNT(pz.id) AS liczba_pozycji
FROM zamowienia z
LEFT JOIN pozycjezamowienia pz ON pz.id_zamowienia = z.id
GROUP BY z.id
HAVING COUNT(pz.id) > 0
LIMIT 10;

-- 6. Sprawdź jakie wartości id_zamowienia są w pozycjezamowienia
SELECT '=== Unikalne wartości id_zamowienia w pozycjezamowienia ===' AS Info;
SELECT DISTINCT id_zamowienia, COUNT(*) AS liczba 
FROM pozycjezamowienia 
GROUP BY id_zamowienia 
ORDER BY id_zamowienia 
LIMIT 20;

-- 7. Sprawdź czy istnieją pozycje z id_zamowienia które nie mają odpowiadającego zamówienia
SELECT '=== Pozycje bez odpowiadającego zamówienia ===' AS Info;
SELECT pz.id, pz.id_zamowienia
FROM pozycjezamowienia pz
LEFT JOIN zamowienia z ON z.id = pz.id_zamowienia
WHERE z.id IS NULL
LIMIT 10;

-- 8. Utwórz klucz obcy (odkomentuj jeśli potrzeba)
-- Najpierw sprawdź jaka jest nazwa kolumny ID w zamowienia
-- Jeśli to nie jest 'id', dostosuj poniższy skrypt

/*
-- Usuń istniejący klucz obcy jeśli istnieje
SET @constraint_name = (
    SELECT CONSTRAINT_NAME 
    FROM information_schema.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'pozycjezamowienia' 
    AND COLUMN_NAME = 'id_zamowienia' 
    AND REFERENCED_TABLE_NAME IS NOT NULL
    LIMIT 1
);

SET @sql = IF(@constraint_name IS NOT NULL, 
    CONCAT('ALTER TABLE pozyjezamowienia DROP FOREIGN KEY ', @constraint_name), 
    'SELECT "Brak istniejącego klucza obcego" AS Info');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Utwórz nowy klucz obcy
-- UWAGA: Dostosuj nazwę kolumny ID w zamowienia do rzeczywistej struktury
ALTER TABLE pozycjezamowienia 
ADD CONSTRAINT fk_pozyjezamowienia_zamowienia 
FOREIGN KEY (id_zamowienia) 
REFERENCES zamowienia(id) 
ON DELETE CASCADE 
ON UPDATE CASCADE;

SELECT 'Klucz obcy został utworzony' AS Info;
*/
