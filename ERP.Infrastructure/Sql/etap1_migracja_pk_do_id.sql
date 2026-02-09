-- =============================================================================
-- ETAP 1 – Migracja PRIMARY KEY → id (tylko tabele BEZ FK)
-- Baza: locbd (MariaDB/MySQL)
-- NIE WYKONUJ przed akceptacją! Wykonuj po jednej tabeli i waliduj.
-- =============================================================================
-- Metoda: CHANGE COLUMN – zachowuje typ, AUTO_INCREMENT, PRIMARY KEY
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) auta
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE auta CHANGE COLUMN id_auta id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 2) awariamaszyna
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE awariamaszyna CHANGE COLUMN id_awaria_maszyna id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 3) awariapozycje
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE awariapozycje CHANGE COLUMN id_awaria_pozycja id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 4) banki
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE banki CHANGE COLUMN id_banku id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 5) daneobliczmaszyne
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE daneobliczmaszyne CHANGE COLUMN id_dane_oblicz_maszyne id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 6) danetabliczka
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE danetabliczka CHANGE COLUMN id_dane_tabliczka id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 7) dokdlaodbiorcy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE dokdlaodbiorcy CHANGE COLUMN id_dok_dla_odbiorcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 8) dostawcy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE dostawcy CHANGE COLUMN id_dostawcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 9) fakturatyp
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE fakturatyp CHANGE COLUMN id_faktura_typ id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 10) foto
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE foto CHANGE COLUMN id_foto id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 11) grupagtu
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE grupagtu CHANGE COLUMN id_grupagtu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 12) grupasprzedazy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE grupasprzedazy CHANGE COLUMN id_grupaprzedazy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 13) grupytowaru
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE grupytowaru CHANGE COLUMN id_grupy_towaru id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 14) jednostki
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE jednostki CHANGE COLUMN id_jednostki id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 15) kalendarz
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE kalendarz CHANGE COLUMN id_kalendarz id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 16) kasa
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE kasa CHANGE COLUMN id_kasy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 17) kurswaluty
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE kurswaluty CHANGE COLUMN id_kurs_waluty id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 18) liniaprodukcyjna
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE liniaprodukcyjna CHANGE COLUMN id_linii_produkcyjnej id int(3) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 19) linieobciazenieoferta
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE linieobciazenieoferta CHANGE COLUMN id_linii_obc_ofe id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 20) linieobciazeniezlecenie
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE linieobciazeniezlecenie CHANGE COLUMN id_obciazenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 21) linieprodukcyjne
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE linieprodukcyjne CHANGE COLUMN id_linii id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 22) linierhproduktu
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE linierhproduktu CHANGE COLUMN id_prod_rh id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 23) maszynatyp
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE maszynatyp CHANGE COLUMN id_typ_maszyny id int(10) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 24) maszyny
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE maszyny CHANGE COLUMN id_maszyny id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 25) narzedziawydane
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE narzedziawydane CHANGE COLUMN id_wydania id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 26) narzedziawydanepracownik
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE narzedziawydanepracownik CHANGE COLUMN id_narzedzia_pracownik id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 27) noty
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE noty CHANGE COLUMN id_noty id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 28) odbiorcagrupa
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE odbiorcagrupa CHANGE COLUMN id_odbiorca_grupa id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 29) odbiorcatyp
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE odbiorcatyp CHANGE COLUMN id_odbiorca_typ id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 30) odbiorcazapisanydogrupy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE odbiorcazapisanydogrupy CHANGE COLUMN id_odbiorca_zapisany_do_grupy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 31) odbiorcy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE odbiorcy CHANGE COLUMN ID_odbiorcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 32) odbiorcylistatemp
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE odbiorcylistatemp CHANGE COLUMN id_odbiorcy_lista_temp id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 33) ofertastatus
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE ofertastatus CHANGE COLUMN id_statusu_oferty id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 34) packinglist
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE packinglist CHANGE COLUMN id_packing_list id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 35) platnosci
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE platnosci CHANGE COLUMN id_platnosci id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 36) platnoscstatus
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE platnoscstatus CHANGE COLUMN id_platnosc_status id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 37) platnosctyp
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE platnosctyp CHANGE COLUMN id_platnosc_typ id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 38) pozycjedelegacji
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE pozycjedelegacji CHANGE COLUMN id_pozycji_delegacji id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 39) pozycjedokdlaodbiorcy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE pozycjedokdlaodbiorcy CHANGE COLUMN id_pozycji_dok_dla_odbiorcy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 40) pozycjepozycjizlecenia
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE pozycjepozycjizlecenia CHANGE COLUMN id_pozycji_pozycji_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 41) pozycjeproduktu
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE pozycjeproduktu CHANGE COLUMN id_pozycji_produktu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 42) pozycjezamowienia
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE pozycjezamowienia CHANGE COLUMN id_pozycji_zamowienia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 43) pozycjezlecenia
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE pozycjezlecenia CHANGE COLUMN ID_pozycji_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 44) pracownicy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE pracownicy CHANGE COLUMN id_pracownika id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 45) premia
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE premia CHANGE COLUMN id_premii id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 46) produkcjastatus
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE produkcjastatus CHANGE COLUMN id_statusu_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 47) produkty
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE produkty CHANGE COLUMN id_produktu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 48) remanent
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE remanent CHANGE COLUMN id_remanentu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 49) rh
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE rh CHANGE COLUMN id_rh id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 50) rhproduktu
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE rhproduktu CHANGE COLUMN id_rhproduktu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 51) rodzajurlopu
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE rodzajurlopu CHANGE COLUMN id_rodzaj_urlopu id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 52) rola
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE rola CHANGE COLUMN id_roli id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 53) serwistyp
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE serwistyp CHANGE COLUMN id_serwistyp id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 54) serwisy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE serwisy CHANGE COLUMN id_serwisu id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 55) serwisypracownicy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE serwisypracownicy CHANGE COLUMN id_serwisy_pracownicy id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 56) skrzynie
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE skrzynie CHANGE COLUMN ID_skrzynia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 57) specmaszklienta
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE specmaszklienta CHANGE COLUMN id_spec_masz_klienta id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 58) specyfikacjamaszyny
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE specyfikacjamaszyny CHANGE COLUMN id_specyfikacja_maszyny id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 59) sprawdzaniemaszyny
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE sprawdzaniemaszyny CHANGE COLUMN id_spradzania_maszyny id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 60) srodkitrwale
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE srodkitrwale CHANGE COLUMN id_srodka_trwalego id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 61) statusinne
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE statusinne CHANGE COLUMN id_statusu_inne id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 62) stawkavat
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE stawkavat CHANGE COLUMN id_stawka_vat id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 63) tabtym
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE tabtym CHANGE COLUMN id_tabtym id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 64) text (nazwa zarezerwowana – używaj backticków)
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE `text` CHANGE COLUMN id_tekst id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 65) towarstatus
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE towarstatus CHANGE COLUMN id_statusu_towaru id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 66) towary
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE towary CHANGE COLUMN ID_towar id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 67) umowypracownicy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE umowypracownicy CHANGE COLUMN id_umowy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 68) urlopy
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE urlopy CHANGE COLUMN id_urlopy id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 69) waluty
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE waluty CHANGE COLUMN id_waluty id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 70) wyplaty
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE wyplaty CHANGE COLUMN id_wyplaty id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 71) wyplatydanem
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE wyplatydanem CHANGE COLUMN id_wyplaty_dane_miesiaca id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 72) wyplatydaney
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE wyplatydaney CHANGE COLUMN id_wyplaty_dane_y id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 73) zamowienia
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE zamowienia CHANGE COLUMN id_zamowienia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 74) zamowieniahala
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE zamowieniahala CHANGE COLUMN id_zamowienia_hala id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 75) zamowieniestatus
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE zamowieniestatus CHANGE COLUMN id_statusu_zamowienia id int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

-- -----------------------------------------------------------------------------
-- 76) zlecenia
-- -----------------------------------------------------------------------------
START TRANSACTION;
ALTER TABLE zlecenia CHANGE COLUMN id_zlecenia id int(15) NOT NULL AUTO_INCREMENT;
COMMIT;
