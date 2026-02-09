-- =============================================================================
-- FIX: Kolumna id w tabelach faktury i pozycjefaktury musi mieć AUTO_INCREMENT
-- Błąd: "Field 'id' doesn't have a default value" przy INSERT (np. Kopiuj do FPF)
-- Przyczyna: po migracji PK z id_faktury/id_pozycji_faktury na id, kolumna id
--            może nie mieć AUTO_INCREMENT (np. gdy migracja była częściowa).
-- =============================================================================
-- Wykonać na bazie ERP przed użyciem funkcji "Kopiuj do FPF".
-- =============================================================================

-- 1) faktury – id musi być AUTO_INCREMENT (INSERT nie podaje id, baza nadaje wartość)
ALTER TABLE faktury MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT;

-- 2) pozycjefaktury – id musi być AUTO_INCREMENT
ALTER TABLE pozycjefaktury MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT;

-- 3) faktury – id_faktur_powiazanych: pole opcjonalne (FK do powiązanych faktur przy korektach)
--    Nowa FPF nie ma powiązań. Zmiana na NULLABLE pozwala na INSERT bez wartości.
ALTER TABLE faktury MODIFY COLUMN id_faktur_powiazanych INT(15) NULL;
