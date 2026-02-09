-- =============================================================================
-- 033: Widok kontrahenci_v (LOCBD) – SELECT z tabeli kontrahenci
-- Standard: id = id, company_id = company_id, kolumna id_firmy obecna
-- =============================================================================

DROP VIEW IF EXISTS kontrahenci_v;

CREATE OR REPLACE VIEW kontrahenci_v AS
SELECT
  kontrahenci.*,
  kontrahenci.id AS id,
  kontrahenci.company_id AS company_id,
  kontrahenci.company_id AS id_firmy
FROM kontrahenci;

