-- =============================================================================
-- 026: Widok aoferty_V – aliasy id i company_id
-- Naprawia błąd "Unknown column 'id' in 'field list'" dla zapytań C# na aoferty_V.
-- Widok zawiera: aoferty.* + id (=ID_oferta) + company_id (=id_firmy).
-- =============================================================================

CREATE OR REPLACE VIEW aoferty_V AS
SELECT
  aoferty.*,
  aoferty.ID_oferta AS id,
  aoferty.id_firmy  AS company_id
FROM aoferty;
