-- Przykładowe skrypty do ustawiania uprawnień operatorów
-- Ten plik zawiera przykłady użycia systemu uprawnień

-- ============================================
-- PRZYKŁAD 1: Podstawowe uprawnienia
-- ============================================

-- Operator ID=1 - pełny dostęp do tabeli Odbiorcy
CALL sp_SetOperatorTablePermission(1, 'Odbiorcy', 1, 1, 1, 1);

-- Operator ID=1 - tylko odczyt (SELECT) do tabeli towary
CALL sp_SetOperatorTablePermission(1, 'towary', 1, 0, 0, 0);

-- Operator ID=1 - odczyt i wstawianie do tabeli magazyn
CALL sp_SetOperatorTablePermission(1, 'magazyn', 1, 1, 0, 0);

-- ============================================
-- PRZYKŁAD 2: Różne poziomy dostępu dla różnych operatorów
-- ============================================

-- Operator ID=2 - administrator (pełny dostęp do wszystkich tabel)
CALL sp_SetOperatorTablePermission(2, 'Odbiorcy', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'towary', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'magazyn', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'operator', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'operator_login', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'operatorfirma', 1, 1, 1, 1);
CALL sp_SetOperatorTablePermission(2, 'operator_table_permissions', 1, 1, 1, 1);

-- Operator ID=3 - użytkownik tylko do odczytu
CALL sp_SetOperatorTablePermission(3, 'Odbiorcy', 1, 0, 0, 0);
CALL sp_SetOperatorTablePermission(3, 'towary', 1, 0, 0, 0);
CALL sp_SetOperatorTablePermission(3, 'magazyn', 1, 0, 0, 0);

-- Operator ID=4 - użytkownik z możliwością dodawania i modyfikacji (bez usuwania)
CALL sp_SetOperatorTablePermission(4, 'Odbiorcy', 1, 1, 1, 0);
CALL sp_SetOperatorTablePermission(4, 'towary', 1, 1, 1, 0);
CALL sp_SetOperatorTablePermission(4, 'magazyn', 1, 1, 1, 0);

-- ============================================
-- PRZYKŁAD 3: Sprawdzanie uprawnień
-- ============================================

-- Sprawdź czy operator ID=1 ma uprawnienie SELECT do tabeli Odbiorcy
CALL sp_CheckOperatorTablePermission(1, 'Odbiorcy', 'SELECT', @has_select);
SELECT @has_select AS 'Czy ma SELECT?';

-- Sprawdź czy operator ID=1 ma uprawnienie DELETE do tabeli Odbiorcy
CALL sp_CheckOperatorTablePermission(1, 'Odbiorcy', 'DELETE', @has_delete);
SELECT @has_delete AS 'Czy ma DELETE?';

-- ============================================
-- PRZYKŁAD 4: Pobieranie wszystkich uprawnień operatora
-- ============================================

-- Pobierz wszystkie uprawnienia operatora ID=1
CALL sp_GetOperatorPermissions(1);

-- ============================================
-- PRZYKŁAD 5: Wyświetlenie podsumowania uprawnień
-- ============================================

-- Wyświetl wszystkie uprawnienia wszystkich operatorów
SELECT * FROM v_operator_permissions_summary;

-- Wyświetl uprawnienia konkretnego operatora
SELECT * FROM v_operator_permissions_summary 
WHERE id_operatora = 1;

-- ============================================
-- PRZYKŁAD 6: Usuwanie uprawnień
-- ============================================

-- Usuń uprawnienia operatora ID=1 do tabeli magazyn
-- CALL sp_RemoveOperatorTablePermission(1, 'magazyn');

-- ============================================
-- PRZYKŁAD 7: Masowe ustawianie uprawnień dla wielu tabel
-- ============================================

-- Funkcja pomocnicza do ustawienia tych samych uprawnień do wielu tabel
-- (można użyć w pętli w aplikacji)

-- Lista tabel do ustawienia uprawnień
-- SET @operator_id = 1;
-- SET @can_select = 1;
-- SET @can_insert = 1;
-- SET @can_update = 1;
-- SET @can_delete = 0;

-- CALL sp_SetOperatorTablePermission(@operator_id, 'Odbiorcy', @can_select, @can_insert, @can_update, @can_delete);
-- CALL sp_SetOperatorTablePermission(@operator_id, 'towary', @can_select, @can_insert, @can_update, @can_delete);
-- CALL sp_SetOperatorTablePermission(@operator_id, 'magazyn', @can_select, @can_insert, @can_update, @can_delete);
