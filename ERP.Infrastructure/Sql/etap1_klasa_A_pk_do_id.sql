-- =============================================================================
-- ETAP 1 – Migracja PK → id (TYLKO KLASA A: incoming_fk=0, outgoing_fk=0)
-- Baza: locbd (MariaDB/MySQL)
-- NIE WYKONUJ automatycznie! Wykonuj blok po bloku i waliduj.
-- =============================================================================
-- Metoda: CHANGE COLUMN – zachowuje typ, NOT NULL, AUTO_INCREMENT, PRIMARY KEY
-- =============================================================================

-- =============================================================================
-- SEKCJA: WYMAGA RĘCZNEJ DECYZJI
-- =============================================================================
-- Tabele POMINIĘTE – nie migrować bez analizy:
--
-- Brak PRIMARY KEY (20 tabel):
--   linieobciazenie, linieobciazenietymcz, linieobciazenietymcz2, maszynadaneobliczen,
--   maszynaodbiorca, maszynaodbiorcapozycja, maszynawymiar, pliksql, pozycjefakturytymcz,
--   pracownicyrh, rh2, rhdostepne, rola_backup, secwin_access, secwin_access_copy,
--   secwin_counters, secwin_licence4, secwin_namecodes, secwin_operators5, secwin_operatorsusergroups
--
-- Złożony PRIMARY KEY / composite (2 tabele z klasy A):
--   magazyn (id_magazyn, id_firmy)
--   pozycjeremanentu (id_pozycji_rem, id_rem)
--
-- PK już = id – pominięte (3 tabele):
--   operator_login, operatorfirma, tym1
-- =============================================================================


-- -----------------------------------------------------------------------------
-- auta (PK: id_auta → id)
-- -----------------------------------------------------------------------------
-- PRZED:
SELECT COUNT(*) AS cnt_przed FROM auta;

START TRANSACTION;
ALTER TABLE auta CHANGE COLUMN id_auta id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- PO:
SELECT COUNT(*) AS cnt_po FROM auta;

-- KONTROLA (PK na id):
SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'auta' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- awariamaszyna (PK: id_awaria_maszyna → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM awariamaszyna;

START TRANSACTION;
ALTER TABLE awariamaszyna CHANGE COLUMN id_awaria_maszyna id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM awariamaszyna;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'awariamaszyna' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- awariapozycje (PK: id_awaria_pozycja → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM awariapozycje;

START TRANSACTION;
ALTER TABLE awariapozycje CHANGE COLUMN id_awaria_pozycja id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM awariapozycje;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'awariapozycje' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- banki (PK: id_banku → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM banki;

START TRANSACTION;
ALTER TABLE banki CHANGE COLUMN id_banku id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM banki;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'banki' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- daneobliczmaszyne (PK: id_dane_oblicz_maszyne → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM daneobliczmaszyne;

START TRANSACTION;
ALTER TABLE daneobliczmaszyne CHANGE COLUMN id_dane_oblicz_maszyne id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM daneobliczmaszyne;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'daneobliczmaszyne' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- danetabliczka (PK: id_dane_tabliczka → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM danetabliczka;

START TRANSACTION;
ALTER TABLE danetabliczka CHANGE COLUMN id_dane_tabliczka id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM danetabliczka;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'danetabliczka' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- dokdlaodbiorcy (PK: id_dok_dla_odbiorcy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM dokdlaodbiorcy;

START TRANSACTION;
ALTER TABLE dokdlaodbiorcy CHANGE COLUMN id_dok_dla_odbiorcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM dokdlaodbiorcy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'dokdlaodbiorcy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- dostawcy (PK: id_dostawcy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM dostawcy;

START TRANSACTION;
ALTER TABLE dostawcy CHANGE COLUMN id_dostawcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM dostawcy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'dostawcy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- fakturatyp (PK: id_faktura_typ → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM fakturatyp;

START TRANSACTION;
ALTER TABLE fakturatyp CHANGE COLUMN id_faktura_typ id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM fakturatyp;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'fakturatyp' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- foto (PK: id_foto → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM foto;

START TRANSACTION;
ALTER TABLE foto CHANGE COLUMN id_foto id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM foto;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'foto' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- grupagtu (PK: id_grupagtu → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM grupagtu;

START TRANSACTION;
ALTER TABLE grupagtu CHANGE COLUMN id_grupagtu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM grupagtu;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'grupagtu' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- grupasprzedazy (PK: id_grupaprzedazy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM grupasprzedazy;

START TRANSACTION;
ALTER TABLE grupasprzedazy CHANGE COLUMN id_grupaprzedazy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM grupasprzedazy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'grupasprzedazy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- grupytowaru (PK: id_grupy_towaru → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM grupytowaru;

START TRANSACTION;
ALTER TABLE grupytowaru CHANGE COLUMN id_grupy_towaru id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM grupytowaru;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'grupytowaru' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- jednostki (PK: id_jednostki → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM jednostki;

START TRANSACTION;
ALTER TABLE jednostki CHANGE COLUMN id_jednostki id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM jednostki;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'jednostki' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- kalendarz (PK: id_kalendarz → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM kalendarz;

START TRANSACTION;
ALTER TABLE kalendarz CHANGE COLUMN id_kalendarz id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM kalendarz;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'kalendarz' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- kasa (PK: id_kasy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM kasa;

START TRANSACTION;
ALTER TABLE kasa CHANGE COLUMN id_kasy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM kasa;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'kasa' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- kurswaluty (PK: id_kurs_waluty → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM kurswaluty;

START TRANSACTION;
ALTER TABLE kurswaluty CHANGE COLUMN id_kurs_waluty id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM kurswaluty;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'kurswaluty' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- liniaprodukcyjna (PK: id_linii_produkcyjnej → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM liniaprodukcyjna;

START TRANSACTION;
ALTER TABLE liniaprodukcyjna CHANGE COLUMN id_linii_produkcyjnej id int(3) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM liniaprodukcyjna;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'liniaprodukcyjna' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- linieobciazenieoferta (PK: id_linii_obc_ofe → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM linieobciazenieoferta;

START TRANSACTION;
ALTER TABLE linieobciazenieoferta CHANGE COLUMN id_linii_obc_ofe id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM linieobciazenieoferta;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'linieobciazenieoferta' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- linieobciazeniezlecenie (PK: id_obciazenia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM linieobciazeniezlecenie;

START TRANSACTION;
ALTER TABLE linieobciazeniezlecenie CHANGE COLUMN id_obciazenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM linieobciazeniezlecenie;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'linieobciazeniezlecenie' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- linieprodukcyjne (PK: id_linii → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM linieprodukcyjne;

START TRANSACTION;
ALTER TABLE linieprodukcyjne CHANGE COLUMN id_linii id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM linieprodukcyjne;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'linieprodukcyjne' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- linierhproduktu (PK: id_prod_rh → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM linierhproduktu;

START TRANSACTION;
ALTER TABLE linierhproduktu CHANGE COLUMN id_prod_rh id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM linierhproduktu;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'linierhproduktu' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- maszynatyp (PK: id_typ_maszyny → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM maszynatyp;

START TRANSACTION;
ALTER TABLE maszynatyp CHANGE COLUMN id_typ_maszyny id int(10) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM maszynatyp;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'maszynatyp' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- maszyny (PK: id_maszyny → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM maszyny;

START TRANSACTION;
ALTER TABLE maszyny CHANGE COLUMN id_maszyny id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM maszyny;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'maszyny' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- narzedziawydane (PK: id_wydania → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM narzedziawydane;

START TRANSACTION;
ALTER TABLE narzedziawydane CHANGE COLUMN id_wydania id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM narzedziawydane;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'narzedziawydane' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- narzedziawydanepracownik (PK: id_narzedzia_pracownik → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM narzedziawydanepracownik;

START TRANSACTION;
ALTER TABLE narzedziawydanepracownik CHANGE COLUMN id_narzedzia_pracownik id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM narzedziawydanepracownik;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'narzedziawydanepracownik' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- noty (PK: id_noty → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM noty;

START TRANSACTION;
ALTER TABLE noty CHANGE COLUMN id_noty id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM noty;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'noty' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- odbiorcagrupa (PK: id_odbiorca_grupa → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM odbiorcagrupa;

START TRANSACTION;
ALTER TABLE odbiorcagrupa CHANGE COLUMN id_odbiorca_grupa id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM odbiorcagrupa;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'odbiorcagrupa' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- odbiorcatyp (PK: id_odbiorca_typ → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM odbiorcatyp;

START TRANSACTION;
ALTER TABLE odbiorcatyp CHANGE COLUMN id_odbiorca_typ id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM odbiorcatyp;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'odbiorcatyp' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- odbiorcazapisanydogrupy (PK: id_odbiorca_zapisany_do_grupy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM odbiorcazapisanydogrupy;

START TRANSACTION;
ALTER TABLE odbiorcazapisanydogrupy CHANGE COLUMN id_odbiorca_zapisany_do_grupy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM odbiorcazapisanydogrupy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'odbiorcazapisanydogrupy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- odbiorcy (PK: ID_odbiorcy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM odbiorcy;

START TRANSACTION;
ALTER TABLE odbiorcy CHANGE COLUMN ID_odbiorcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM odbiorcy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'odbiorcy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- odbiorcylistatemp (PK: id_odbiorcy_lista_temp → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM odbiorcylistatemp;

START TRANSACTION;
ALTER TABLE odbiorcylistatemp CHANGE COLUMN id_odbiorcy_lista_temp id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM odbiorcylistatemp;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'odbiorcylistatemp' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- ofertastatus (PK: id_statusu_oferty → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM ofertastatus;

START TRANSACTION;
ALTER TABLE ofertastatus CHANGE COLUMN id_statusu_oferty id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM ofertastatus;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ofertastatus' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- operator_login – POMINIĘTE: PK już = id
-- -----------------------------------------------------------------------------
-- SELECT COUNT(*) FROM operator_login;
-- PK już nazywa się 'id' – brak zmian.


-- -----------------------------------------------------------------------------
-- operatorfirma – POMINIĘTE: PK już = id
-- -----------------------------------------------------------------------------
-- PK już nazywa się 'id' – brak zmian.


-- -----------------------------------------------------------------------------
-- packinglist (PK: id_packing_list → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM packinglist;

START TRANSACTION;
ALTER TABLE packinglist CHANGE COLUMN id_packing_list id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM packinglist;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'packinglist' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- platnosci (PK: id_platnosci → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM platnosci;

START TRANSACTION;
ALTER TABLE platnosci CHANGE COLUMN id_platnosci id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM platnosci;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'platnosci' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- platnoscstatus (PK: id_platnosc_status → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM platnoscstatus;

START TRANSACTION;
ALTER TABLE platnoscstatus CHANGE COLUMN id_platnosc_status id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM platnoscstatus;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'platnoscstatus' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- platnosctyp (PK: id_platnosc_typ → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM platnosctyp;

START TRANSACTION;
ALTER TABLE platnosctyp CHANGE COLUMN id_platnosc_typ id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM platnosctyp;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'platnosctyp' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- pozycjedelegacji (PK: id_pozycji_delegacji → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM pozycjedelegacji;

START TRANSACTION;
ALTER TABLE pozycjedelegacji CHANGE COLUMN id_pozycji_delegacji id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM pozycjedelegacji;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjedelegacji' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- pozycjedokdlaodbiorcy (PK: id_pozycji_dok_dla_odbiorcy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM pozycjedokdlaodbiorcy;

START TRANSACTION;
ALTER TABLE pozycjedokdlaodbiorcy CHANGE COLUMN id_pozycji_dok_dla_odbiorcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM pozycjedokdlaodbiorcy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjedokdlaodbiorcy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- pozycjepozycjizlecenia (PK: id_pozycji_pozycji_zlecenia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM pozycjepozycjizlecenia;

START TRANSACTION;
ALTER TABLE pozycjepozycjizlecenia CHANGE COLUMN id_pozycji_pozycji_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM pozycjepozycjizlecenia;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjepozycjizlecenia' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- pozycjeproduktu (PK: id_pozycji_produktu → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM pozycjeproduktu;

START TRANSACTION;
ALTER TABLE pozycjeproduktu CHANGE COLUMN id_pozycji_produktu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM pozycjeproduktu;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjeproduktu' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- pozycjezamowienia (PK: id_pozycji_zamowienia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM pozycjezamowienia;

START TRANSACTION;
ALTER TABLE pozycjezamowienia CHANGE COLUMN id_pozycji_zamowienia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM pozycjezamowienia;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjezamowienia' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- pozycjezlecenia (PK: ID_pozycji_zlecenia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM pozycjezlecenia;

START TRANSACTION;
ALTER TABLE pozycjezlecenia CHANGE COLUMN ID_pozycji_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM pozycjezlecenia;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pozycjezlecenia' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- pracownicy (PK: id_pracownika → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM pracownicy;

START TRANSACTION;
ALTER TABLE pracownicy CHANGE COLUMN id_pracownika id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM pracownicy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'pracownicy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- premia (PK: id_premii → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM premia;

START TRANSACTION;
ALTER TABLE premia CHANGE COLUMN id_premii id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM premia;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'premia' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- produkcjastatus (PK: id_statusu_zlecenia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM produkcjastatus;

START TRANSACTION;
ALTER TABLE produkcjastatus CHANGE COLUMN id_statusu_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM produkcjastatus;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'produkcjastatus' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- produkty (PK: id_produktu → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM produkty;

START TRANSACTION;
ALTER TABLE produkty CHANGE COLUMN id_produktu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM produkty;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'produkty' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- remanent (PK: id_remanentu → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM remanent;

START TRANSACTION;
ALTER TABLE remanent CHANGE COLUMN id_remanentu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM remanent;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'remanent' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- rh (PK: id_rh → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM rh;

START TRANSACTION;
ALTER TABLE rh CHANGE COLUMN id_rh id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM rh;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'rh' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- rhproduktu (PK: id_rhproduktu → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM rhproduktu;

START TRANSACTION;
ALTER TABLE rhproduktu CHANGE COLUMN id_rhproduktu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM rhproduktu;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'rhproduktu' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- rodzajurlopu (PK: id_rodzaj_urlopu → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM rodzajurlopu;

START TRANSACTION;
ALTER TABLE rodzajurlopu CHANGE COLUMN id_rodzaj_urlopu id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM rodzajurlopu;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'rodzajurlopu' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- rola (PK: id_roli → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM rola;

START TRANSACTION;
ALTER TABLE rola CHANGE COLUMN id_roli id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM rola;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'rola' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- serwistyp (PK: id_serwistyp → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM serwistyp;

START TRANSACTION;
ALTER TABLE serwistyp CHANGE COLUMN id_serwistyp id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM serwistyp;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'serwistyp' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- serwisy (PK: id_serwisu → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM serwisy;

START TRANSACTION;
ALTER TABLE serwisy CHANGE COLUMN id_serwisu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM serwisy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'serwisy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- serwisypracownicy (PK: id_serwisy_pracownicy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM serwisypracownicy;

START TRANSACTION;
ALTER TABLE serwisypracownicy CHANGE COLUMN id_serwisy_pracownicy id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM serwisypracownicy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'serwisypracownicy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- skrzynie (PK: ID_skrzynia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM skrzynie;

START TRANSACTION;
ALTER TABLE skrzynie CHANGE COLUMN ID_skrzynia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM skrzynie;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'skrzynie' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- specmaszklienta (PK: id_spec_masz_klienta → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM specmaszklienta;

START TRANSACTION;
ALTER TABLE specmaszklienta CHANGE COLUMN id_spec_masz_klienta id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM specmaszklienta;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'specmaszklienta' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- specyfikacjamaszyny (PK: id_specyfikacja_maszyny → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM specyfikacjamaszyny;

START TRANSACTION;
ALTER TABLE specyfikacjamaszyny CHANGE COLUMN id_specyfikacja_maszyny id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM specyfikacjamaszyny;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'specyfikacjamaszyny' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- sprawdzaniemaszyny (PK: id_spradzania_maszyny → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM sprawdzaniemaszyny;

START TRANSACTION;
ALTER TABLE sprawdzaniemaszyny CHANGE COLUMN id_spradzania_maszyny id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM sprawdzaniemaszyny;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sprawdzaniemaszyny' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- srodkitrwale (PK: id_srodka_trwalego → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM srodkitrwale;

START TRANSACTION;
ALTER TABLE srodkitrwale CHANGE COLUMN id_srodka_trwalego id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM srodkitrwale;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'srodkitrwale' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- statusinne (PK: id_statusu_inne → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM statusinne;

START TRANSACTION;
ALTER TABLE statusinne CHANGE COLUMN id_statusu_inne id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM statusinne;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'statusinne' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- stawkavat (PK: id_stawka_vat → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM stawkavat;

START TRANSACTION;
ALTER TABLE stawkavat CHANGE COLUMN id_stawka_vat id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM stawkavat;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'stawkavat' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- tabtym (PK: id_tabtym → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM tabtym;

START TRANSACTION;
ALTER TABLE tabtym CHANGE COLUMN id_tabtym id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM tabtym;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'tabtym' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- test (PK: ID → id) – uwaga: kolumna ID, zmiana na id
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM test;

START TRANSACTION;
ALTER TABLE test CHANGE COLUMN ID id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM test;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'test' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- text (PK: id_tekst → id) – nazwa tabeli zarezerwowana, używaj backticków
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM `text`;

START TRANSACTION;
ALTER TABLE `text` CHANGE COLUMN id_tekst id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM `text`;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'text' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- towarstatus (PK: id_statusu_towaru → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM towarstatus;

START TRANSACTION;
ALTER TABLE towarstatus CHANGE COLUMN id_statusu_towaru id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM towarstatus;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'towarstatus' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- towary (PK: ID_towar → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM towary;

START TRANSACTION;
ALTER TABLE towary CHANGE COLUMN ID_towar id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM towary;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'towary' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- tym1 – POMINIĘTE: PK już = id
-- -----------------------------------------------------------------------------
-- PK już nazywa się 'id' – brak zmian.


-- -----------------------------------------------------------------------------
-- umowypracownicy (PK: id_umowy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM umowypracownicy;

START TRANSACTION;
ALTER TABLE umowypracownicy CHANGE COLUMN id_umowy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM umowypracownicy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'umowypracownicy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- urlopy (PK: id_urlopy → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM urlopy;

START TRANSACTION;
ALTER TABLE urlopy CHANGE COLUMN id_urlopy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM urlopy;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'urlopy' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- waluty (PK: id_waluty → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM waluty;

START TRANSACTION;
ALTER TABLE waluty CHANGE COLUMN id_waluty id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM waluty;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'waluty' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- wyplaty (PK: id_wyplaty → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM wyplaty;

START TRANSACTION;
ALTER TABLE wyplaty CHANGE COLUMN id_wyplaty id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM wyplaty;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'wyplaty' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- wyplatydanem (PK: id_wyplaty_dane_miesiaca → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM wyplatydanem;

START TRANSACTION;
ALTER TABLE wyplatydanem CHANGE COLUMN id_wyplaty_dane_miesiaca id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM wyplatydanem;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'wyplatydanem' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- wyplatydaney (PK: id_wyplaty_dane_y → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM wyplatydaney;

START TRANSACTION;
ALTER TABLE wyplatydaney CHANGE COLUMN id_wyplaty_dane_y id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM wyplatydaney;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'wyplatydaney' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- zamowienia (PK: id_zamowienia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM zamowienia;

START TRANSACTION;
ALTER TABLE zamowienia CHANGE COLUMN id_zamowienia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM zamowienia;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'zamowienia' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- zamowieniahala (PK: id_zamowienia_hala → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM zamowieniahala;

START TRANSACTION;
ALTER TABLE zamowieniahala CHANGE COLUMN id_zamowienia_hala id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM zamowieniahala;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'zamowieniahala' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- zamowieniestatus (PK: id_statusu_zamowienia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM zamowieniestatus;

START TRANSACTION;
ALTER TABLE zamowieniestatus CHANGE COLUMN id_statusu_zamowienia id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM zamowieniestatus;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'zamowieniestatus' AND COLUMN_NAME = 'id';


-- -----------------------------------------------------------------------------
-- zlecenia (PK: id_zlecenia → id)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS cnt_przed FROM zlecenia;

START TRANSACTION;
ALTER TABLE zlecenia CHANGE COLUMN id_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

SELECT COUNT(*) AS cnt_po FROM zlecenia;

SELECT COLUMN_NAME, COLUMN_KEY, EXTRA FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'zlecenia' AND COLUMN_NAME = 'id';


-- =============================================================================
-- TABELE SYSTEMOWE / SPECJALNE – opcjonalnie (sprawdź przed wykonaniem)
-- =============================================================================
-- ar_internal_metadata: PK=key (varchar) – tabela Rails, zmiana może wpłynąć na framework
-- schema_migrations: PK=version (varchar) – tabela Rails, NIE ZMIENIAĆ
-- =============================================================================
