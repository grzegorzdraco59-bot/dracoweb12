-- =============================================================================
-- 029: Widok dostawcy_V – SELECT z widoku, zapis do tabeli dostawcy
-- Standard: id = id_dostawcy, company_id = id_firmy, reszta pól 1:1
-- =============================================================================

CREATE OR REPLACE VIEW dostawcy_V AS
SELECT
  dostawcy.*,
  dostawcy.id_dostawcy AS id,
  dostawcy.id_firmy AS company_id
FROM dostawcy;
