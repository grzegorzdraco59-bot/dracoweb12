-- =============================================================================
-- Weryfikacja schematu tabel aoferty i apozycjeoferty
-- Uruchom na docelowej bazie przed synchronizacją kodu.
-- =============================================================================

SELECT '=== aoferty: kolumny ===' AS info;
SHOW COLUMNS FROM aoferty;

SELECT '=== apozycjeoferty: kolumny ===' AS info;
SHOW COLUMNS FROM apozycjeoferty;

-- Sprawdzenie kluczowych kolumn (wynik: 1 = istnieje, 0 = brak)
SELECT 
  (SELECT COUNT(*) FROM information_schema.COLUMNS 
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'aoferty' AND COLUMN_NAME IN ('id_oferta', 'ID_oferta')) AS aoferty_ma_pk,
  (SELECT COUNT(*) FROM information_schema.COLUMNS 
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'aoferty' AND COLUMN_NAME IN ('sum_brutto', 'sum_netto', 'sum_vat')) AS aoferty_ma_sum,
  (SELECT COUNT(*) FROM information_schema.COLUMNS 
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'aoferty' AND COLUMN_NAME = 'status') AS aoferty_ma_status,
  (SELECT COUNT(*) FROM information_schema.COLUMNS 
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'apozycjeoferty' AND COLUMN_NAME IN ('oferta_id', 'ID_oferta')) AS apozycje_ma_fk_oferta,
  (SELECT COUNT(*) FROM information_schema.COLUMNS 
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'apozycjeoferty' AND COLUMN_NAME IN ('ilosc', 'Sztuki')) AS apozycje_ma_ilosc,
  (SELECT COUNT(*) FROM information_schema.COLUMNS 
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'apozycjeoferty' AND COLUMN_NAME IN ('cena_netto', 'Cena')) AS apozycje_ma_cena,
  (SELECT COUNT(*) FROM information_schema.COLUMNS 
   WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'apozycjeoferty' AND COLUMN_NAME IN ('brutto_poz', 'cena_brutto')) AS apozycje_ma_brutto;
