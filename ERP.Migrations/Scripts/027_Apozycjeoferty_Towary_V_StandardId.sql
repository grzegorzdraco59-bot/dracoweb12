-- =============================================================================
-- 027: Widoki apozycjeoferty_V i towary_V – standaryzacja id
-- id = PK rekordu widoku, FK pod swoimi nazwami + opcjonalne aliasy *_id
-- =============================================================================

-- Widok pozycji oferty: id = PK pozycji, id_towaru = FK, towar_id = alias
CREATE OR REPLACE VIEW apozycjeoferty_V AS
SELECT
  apozycjeoferty.*,
  apozycjeoferty.id_pozycja_oferty AS id,
  apozycjeoferty.id_towaru AS towar_id
FROM apozycjeoferty;

-- Widok towarów: id = PK towaru (id_towar)
CREATE OR REPLACE VIEW towary_V AS
SELECT
  towary.*,
  towary.id_towar AS id
FROM towary;
