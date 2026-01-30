-- FAZA 3B / KROK 4: Soft delete (IsActive) dla operatorfirma
-- Uruchomić w HeidiSQL na właściwej bazie.
-- Tabela: operatorfirma. Kolumny: id, id_operatora, id_firmy, rola.

ALTER TABLE operatorfirma
ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1;
