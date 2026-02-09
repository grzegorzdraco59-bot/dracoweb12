-- FULLTEXT search dla tabeli aoferty (MariaDB 10.6+, InnoDB)
-- Kolumny tekstowe: CHAR, VARCHAR, TEXT (pomijamy: daty, liczby, bit/bool, id, FK)
--
-- Wymagana migracja: faza4_krok2_status_oferty_zamowienia.sql (kolumna status)
-- Jeśli status nie istnieje, usuń ją z listy lub dodaj: ALTER TABLE aoferty ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'Draft';
--
-- Uwaga: Jeśli indeks ft_aoferty_alltext już istnieje:
--   ALTER TABLE aoferty DROP INDEX ft_aoferty_alltext;

ALTER TABLE aoferty
  ADD FULLTEXT INDEX ft_aoferty_alltext (
    odbiorca_nazwa,
    odbiorca_ulica,
    odbiorca_kod_poczt,
    odbiorca_miasto,
    odbiorca_panstwo,
    odbiorca_nip,
    odbiorca_mail,
    Waluta,
    uwagi_do_oferty,
    dane_dodatkowe,
    operator,
    uwagi_targi,
    historia,
    status
  );
