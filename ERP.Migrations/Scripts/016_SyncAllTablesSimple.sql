-- Prostsza wersja skryptu do synchronizacji wszystkich tabel
-- Najpierw wyświetla listę tabel, które będą synchronizowane
-- Następnie można uruchomić synchronizację dla wybranych tabel

-- ============================================
-- KROK 1: Sprawdź które tabele istnieją w obu bazach
-- ============================================
SELECT 
    t.TABLE_NAME,
    (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE 
     WHERE TABLE_SCHEMA = 'localbddraco' 
     AND TABLE_NAME = t.TABLE_NAME 
     AND CONSTRAINT_NAME = 'PRIMARY') AS has_primary_key,
    (SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE 
     WHERE TABLE_SCHEMA = 'localbddraco' 
     AND TABLE_NAME = t.TABLE_NAME 
     AND CONSTRAINT_NAME = 'PRIMARY' 
     LIMIT 1) AS primary_key_column,
    (SELECT COUNT(*) FROM localbddraco.`operator`) AS src_count_example
FROM information_schema.TABLES t
WHERE t.TABLE_SCHEMA = 'localbddraco'
AND t.TABLE_TYPE = 'BASE TABLE'
AND EXISTS (
    SELECT 1 
    FROM information_schema.TABLES t2 
    WHERE t2.TABLE_SCHEMA = 'locbd' 
    AND t2.TABLE_NAME = t.TABLE_NAME
)
ORDER BY t.TABLE_NAME;

-- ============================================
-- KROK 2: Dla każdej tabeli - sprawdź ile rekordów do dodania
-- ============================================
-- Poniżej znajdują się przykładowe zapytania dla głównych tabel
-- Możesz je uruchomić osobno lub zmodyfikować dla innych tabel

-- Przykład dla tabeli operator (locbd.operator ma id, localbddraco ma id_operatora):
/*
SELECT 
    'operator' AS tabela,
    COUNT(*) AS rekordy_do_dodania
FROM localbddraco.operator src
WHERE NOT EXISTS (
    SELECT 1 
    FROM locbd.operator dest 
    WHERE dest.id = src.id_operatora
);
*/

-- ============================================
-- KROK 3: Synchronizacja - użyj skryptu 015_SyncAllTablesFromLocalbddraco.sql
-- lub uruchom poniższe zapytania dla konkretnych tabel
-- ============================================

-- UWAGA: Poniższe zapytania są przykładami. 
-- Dla pełnej automatycznej synchronizacji użyj skryptu 015_SyncAllTablesFromLocalbddraco.sql

-- Przykład synchronizacji dla tabeli operator (locbd.operator.id AUTO_INCREMENT):
/*
INSERT INTO locbd.operator (
    id_firmy, imie_nazwisko, uprawnienia,
    senderEmail, senderUserName, senderEmailServer, 
    senderEmailPassword, messageText, ccAdresse
)
SELECT 
    src.id_firmy, src.imie_nazwisko, src.uprawnienia,
    src.senderEmail, src.senderUserName, src.senderEmailServer,
    src.senderEmailPassword, src.messageText, src.ccAdresse
FROM localbddraco.operator src
WHERE NOT EXISTS (
    SELECT 1 
    FROM locbd.operator dest 
    WHERE dest.id = src.id_operatora
);
*/

-- Przykład synchronizacji dla tabeli Odbiorcy (po migracji PK: id):
/*
INSERT INTO locbd.Odbiorcy (
    id, id_firmy, Nazwa, Nazwisko, Imie, Uwagi,
    Tel_1, Tel_2, NIP, Ulica_nr, Kod_pocztowy, Miasto, Kraj,
    Ulica_nr_wysylka, Kod_pocztowy_wysylka, Miasto_wysylka, Kraj_wysylka,
    Email_1, Email_2, kod, status, waluta, odbiorca_typ,
    do_oferty, status_vat, regon, adres_caly
)
SELECT 
    src.id, src.id_firmy, src.Nazwa, src.Nazwisko, src.Imie, src.Uwagi,
    src.Tel_1, src.Tel_2, src.NIP, src.Ulica_nr, src.Kod_pocztowy, src.Miasto, src.Kraj,
    src.Ulica_nr_wysylka, src.Kod_pocztowy_wysylka, src.Miasto_wysylka, src.Kraj_wysylka,
    src.Email_1, src.Email_2, src.kod, src.status, src.waluta, src.odbiorca_typ,
    src.do_oferty, src.status_vat, src.regon, src.adres_caly
FROM localbddraco.Odbiorcy src
WHERE NOT EXISTS (
    SELECT 1 
    FROM locbd.Odbiorcy dest 
    WHERE dest.id = src.id
);
*/
