-- =============================================================================
-- Kolumny SMTP w tabeli firmy (per firma). PK: id.
-- Używane przez "Oferta mail" – wysyłka e-mail SMTP z ustawień wybranej firmy.
-- Dla MySQL < 8.0.12 (brak IF NOT EXISTS) wykonaj po kolei pojedyncze ALTER:
--   ALTER TABLE firmy ADD COLUMN smtp_host VARCHAR(255) NULL;
--   ALTER TABLE firmy ADD COLUMN smtp_port INT NULL DEFAULT 25;
--   ... itd.
-- =============================================================================

ALTER TABLE firmy
  ADD COLUMN IF NOT EXISTS smtp_host       VARCHAR(255) NULL,
  ADD COLUMN IF NOT EXISTS smtp_port       INT NULL DEFAULT 25,
  ADD COLUMN IF NOT EXISTS smtp_user       VARCHAR(255) NULL,
  ADD COLUMN IF NOT EXISTS smtp_pass       VARCHAR(255) NULL,
  ADD COLUMN IF NOT EXISTS smtp_ssl        TINYINT(1) NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS smtp_from_email VARCHAR(255) NULL,
  ADD COLUMN IF NOT EXISTS smtp_from_name  VARCHAR(255) NULL;
