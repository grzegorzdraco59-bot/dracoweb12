-- =============================================================================
-- 023: Audyt struktury bazy dla dopasowania Clarion DCT
-- Uruchomić na locbd przed audytem słownika Clarion
-- =============================================================================

-- 1) PK i AUTO_INCREMENT dla wszystkich tabel
SELECT '=== 1) PK i AUTO_INCREMENT ===' AS sekcja;
SELECT t.TABLE_NAME, k.COLUMN_NAME AS pk_column, c.COLUMN_TYPE, c.EXTRA, t.AUTO_INCREMENT
FROM information_schema.TABLES t
LEFT JOIN information_schema.KEY_COLUMN_USAGE k 
  ON k.TABLE_SCHEMA = t.TABLE_SCHEMA AND k.TABLE_NAME = t.TABLE_NAME AND k.CONSTRAINT_NAME = 'PRIMARY'
LEFT JOIN information_schema.COLUMNS c 
  ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME AND c.COLUMN_NAME = k.COLUMN_NAME
WHERE t.TABLE_SCHEMA = 'locbd' AND t.TABLE_TYPE = 'BASE TABLE'
ORDER BY t.TABLE_NAME;

-- 2) Tabele z złożonym PK (bez AUTO_INCREMENT na pojedynczej kolumnie)
SELECT '=== 2) Złożony PRIMARY KEY ===' AS sekcja;
SELECT k.TABLE_NAME, GROUP_CONCAT(k.COLUMN_NAME ORDER BY k.ORDINAL_POSITION) AS pk_columns
FROM information_schema.KEY_COLUMN_USAGE k
WHERE k.TABLE_SCHEMA = 'locbd' AND k.CONSTRAINT_NAME = 'PRIMARY'
GROUP BY k.TABLE_SCHEMA, k.TABLE_NAME
HAVING COUNT(*) > 1;

-- 3) Kolumny FK (logiczne) w tabelach pozycji – do weryfikacji nazw
SELECT '=== 3) Kolumny oferta_id / id_faktury / id_zamowienia ===' AS sekcja;
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'locbd'
  AND (
    COLUMN_NAME IN ('oferta_id', 'ID_oferta', 'id_faktury', 'id_zamowienia', 'ID_zlecenia', 'id_zlecenia')
  )
ORDER BY TABLE_NAME, COLUMN_NAME;

-- 4) Widoki (tylko SELECT)
SELECT '=== 4) Widoki ===' AS sekcja;
SELECT TABLE_NAME FROM information_schema.VIEWS WHERE TABLE_SCHEMA = 'locbd' ORDER BY TABLE_NAME;
