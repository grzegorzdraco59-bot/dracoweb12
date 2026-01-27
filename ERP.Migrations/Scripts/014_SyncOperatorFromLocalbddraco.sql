-- Skrypt do synchronizacji brakujących rekordów z localbddraco.operator do locbd.operator
-- Wstawia tylko te rekordy, które istnieją w localbddraco ale nie istnieją w locbd
-- Porównanie odbywa się na podstawie id_operatora

-- Krok 1: Sprawdź ile rekordów zostanie dodanych (tylko informacyjnie)
SELECT 
    COUNT(*) AS rekordy_do_dodania
FROM localbddraco.operator src
WHERE NOT EXISTS (
    SELECT 1 
    FROM locbd.operator dest 
    WHERE dest.id_operatora = src.id_operatora
);

-- Krok 2: Wstaw brakujące rekordy z localbddraco do locbd
-- Uwaga: id_operatora jest kopiowane z source, aby zachować spójność między bazami
INSERT INTO locbd.operator (
    id_operatora,
    id_firmy,
    imie_nazwisko,
    uprawnienia,
    senderEmail,
    senderUserName,
    senderEmailServer,
    senderEmailPassword,
    messageText,
    ccAdresse
)
SELECT 
    src.id_operatora,
    src.id_firmy,
    src.imie_nazwisko,
    src.uprawnienia,
    src.senderEmail,
    src.senderUserName,
    src.senderEmailServer,
    src.senderEmailPassword,
    src.messageText,
    src.ccAdresse
FROM localbddraco.operator src
WHERE NOT EXISTS (
    SELECT 1 
    FROM locbd.operator dest 
    WHERE dest.id_operatora = src.id_operatora
);

-- Krok 3: Sprawdź ile rekordów zostało dodanych
SELECT 
    ROW_COUNT() AS dodane_rekordy;

-- Krok 4: Ustaw AUTO_INCREMENT na właściwą wartość (następna wartość powinna być wyższa niż maksymalne id_operatora)
SET @max_id = (SELECT COALESCE(MAX(id_operatora), 0) FROM locbd.operator);
SET @sql = CONCAT('ALTER TABLE locbd.operator AUTO_INCREMENT = ', @max_id + 1);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Krok 5: Sprawdź czy wszystkie rekordy zostały zsynchronizowane
SELECT 
    (SELECT COUNT(*) FROM localbddraco.operator) AS rekordy_w_localbddraco,
    (SELECT COUNT(*) FROM locbd.operator) AS rekordy_w_locbd,
    (SELECT COUNT(*) FROM localbddraco.operator) - 
    (SELECT COUNT(*) FROM locbd.operator) AS roznica;
