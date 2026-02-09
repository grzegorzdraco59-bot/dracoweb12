-- =============================================================================
-- 032: Widok pozycjezamowienia_V – SELECT z widoku, zapis do tabeli pozycjezamowienia
-- Standard: id = id_pozycji_zamowienia, company_id = id_firmy
-- =============================================================================

CREATE OR REPLACE VIEW pozycjezamowienia_V AS
SELECT
  p.*,
  p.id_pozycji_zamowienia AS id,
  p.id_firmy              AS company_id
FROM pozycjezamowienia p;
