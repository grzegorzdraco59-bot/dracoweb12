-- =============================================================================
-- 031: Widok zamowienia_V – jawna lista kolumn z aliasami pod UI
-- Kolumny: id_zam, data_zam, nr, st, sk, dostawca_nazwa, waluta, data_dost,
-- status_zam, status_platnosci, data_platnos, nr_faktury, data_faktury,
-- wartosc, uwagi, dla_kogo, tabela_nbp, kurs_v
-- Sort domyślny: ORDER BY data_zamowienia DESC, nr_zamowienia DESC
-- =============================================================================

CREATE OR REPLACE VIEW zamowienia_V AS
SELECT
  z.id_zamowienia,
  z.id_firmy,
  z.data_zamowienia,
  z.nr_zamowienia,
  z.status_zamowienia,
  z.id_dostawcy,
  z.dostawca,
  z.waluta,
  z.tabela_nbp,
  z.kurs_waluty,
  z.data_dostawy,
  z.nr_faktury,
  z.status_platnosci,
  z.wartosc,
  z.dla_kogo,
  z.operator,
  z.data_platnosci,
  z.uwagi_zam,
  z.data_faktury,
  z.termin_platnosci,
  z.dostawca_mail,
  z.data_tabeli_nbp,
  z.skopiowano_niedostarczone,
  z.skopiowano_do_magazynu,
  -- aliasy pod bindy UI
  z.id_zamowienia     AS id,
  z.id_zamowienia     AS id_zam,
  z.id_firmy          AS company_id,
  z.data_zamowienia   AS data_zam,
  z.nr_zamowienia     AS nr,
  z.status_zamowienia AS st,
  '' AS sk,
  z.dostawca          AS dostawca_nazwa,
  z.data_dostawy      AS data_dost,
  z.status_zamowienia AS status_zam,
  z.status_platnosci  AS status_platnosci,
  z.data_platnosci    AS data_platnos,
  z.nr_faktury        AS nr_faktury,
  z.data_faktury      AS data_faktury,
  z.wartosc           AS wartosc,
  z.uwagi_zam AS uwagi,
  z.dla_kogo          AS dla_kogo,
  z.tabela_nbp        AS tabela_nbp,
  z.kurs_waluty       AS kurs_v
FROM zamowienia z;
