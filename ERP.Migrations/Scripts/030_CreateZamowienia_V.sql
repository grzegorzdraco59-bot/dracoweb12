-- =============================================================================
-- 030: Widok zamowienia_V – SELECT z widoku, zapis do tabeli zamowienia
-- Standard: id = id_zamowienia, company_id = id_firmy, reszta pól 1:1
-- =============================================================================

CREATE OR REPLACE VIEW zamowienia_V AS
SELECT
  zamowienia.*,
  zamowienia.id_zamowienia AS id,
  zamowienia.id_firmy AS company_id
FROM zamowienia;
