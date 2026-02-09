=== ETAP 1 – Analiza tabel pod migrację PK → id ===

Data: 2026-02-02 18:17:10

## 1) Tabele z FK wychodzącymi (POMINIĘTE)

doc_counters, ofertypozycje, operator_table_permissions, pozycjefaktury

## 2) Tabele referencjonowane przez inne (POMINIĘTE)

faktury, firmy, oferty, operator

## 3) Tabele spełniające warunki ETAPU 1

| Tabela | PK (obecna) | Typ | AUTO_INCREMENT |
|--------|-------------|-----|----------------|
| auta | id_auta | int(11) | TAK |
| awariamaszyna | id_awaria_maszyna | int(11) | TAK |
| awariapozycje | id_awaria_pozycja | int(15) | TAK |
| banki | id_banku | int(15) | TAK |
| daneobliczmaszyne | id_dane_oblicz_maszyne | int(11) | TAK |
| danetabliczka | id_dane_tabliczka | int(11) | TAK |
| dokdlaodbiorcy | id_dok_dla_odbiorcy | int(15) | TAK |
| dostawcy | id_dostawcy | int(15) | TAK |
| fakturatyp | id_faktura_typ | int(15) | TAK |
| foto | id_foto | int(15) | TAK |
| grupagtu | id_grupagtu | int(15) | TAK |
| grupasprzedazy | id_grupaprzedazy | int(15) | TAK |
| grupytowaru | id_grupy_towaru | int(15) | TAK |
| jednostki | id_jednostki | int(15) | TAK |
| kalendarz | id_kalendarz | int(11) | TAK |
| kasa | id_kasy | int(15) | TAK |
| kurswaluty | id_kurs_waluty | int(15) | TAK |
| liniaprodukcyjna | id_linii_produkcyjnej | int(3) | TAK |
| linieobciazenieoferta | id_linii_obc_ofe | int(11) | TAK |
| linieobciazeniezlecenie | id_obciazenia | int(15) | TAK |
| linieprodukcyjne | id_linii | int(15) | TAK |
| linierhproduktu | id_prod_rh | int(11) | TAK |
| maszynatyp | id_typ_maszyny | int(10) | TAK |
| maszyny | id_maszyny | int(11) | TAK |
| narzedziawydane | id_wydania | int(15) | TAK |
| narzedziawydanepracownik | id_narzedzia_pracownik | int(15) | TAK |
| noty | id_noty | int(11) | TAK |
| odbiorcagrupa | id_odbiorca_grupa | int(15) | TAK |
| odbiorcatyp | id_odbiorca_typ | int(11) | TAK |
| odbiorcazapisanydogrupy | id_odbiorca_zapisany_do_grupy | int(15) | TAK |
| odbiorcy | ID_odbiorcy | int(15) | TAK |
| odbiorcylistatemp | id_odbiorcy_lista_temp | int(15) | TAK |
| ofertastatus | id_statusu_oferty | int(11) | TAK |
| packinglist | id_packing_list | int(11) | TAK |
| platnosci | id_platnosci | int(15) | TAK |
| platnoscstatus | id_platnosc_status | int(15) | TAK |
| platnosctyp | id_platnosc_typ | int(11) | TAK |
| pozycjedelegacji | id_pozycji_delegacji | int(15) | TAK |
| pozycjedokdlaodbiorcy | id_pozycji_dok_dla_odbiorcy | int(15) | TAK |
| pozycjepozycjizlecenia | id_pozycji_pozycji_zlecenia | int(15) | TAK |
| pozycjeproduktu | id_pozycji_produktu | int(15) | TAK |
| pozycjezamowienia | id_pozycji_zamowienia | int(15) | TAK |
| pozycjezlecenia | ID_pozycji_zlecenia | int(15) | TAK |
| pracownicy | id_pracownika | int(15) | TAK |
| premia | id_premii | int(11) | TAK |
| produkcjastatus | id_statusu_zlecenia | int(15) | TAK |
| produkty | id_produktu | int(15) | TAK |
| remanent | id_remanentu | int(15) | TAK |
| rh | id_rh | int(11) | TAK |
| rhproduktu | id_rhproduktu | int(15) | TAK |
| rodzajurlopu | id_rodzaj_urlopu | int(11) | TAK |
| rola | id_roli | int(11) | TAK |
| serwistyp | id_serwistyp | int(11) | TAK |
| serwisy | id_serwisu | int(15) | TAK |
| serwisypracownicy | id_serwisy_pracownicy | int(11) | TAK |
| skrzynie | ID_skrzynia | int(15) | TAK |
| specmaszklienta | id_spec_masz_klienta | int(11) | TAK |
| specyfikacjamaszyny | id_specyfikacja_maszyny | int(15) | TAK |
| sprawdzaniemaszyny | id_spradzania_maszyny | int(11) | TAK |
| srodkitrwale | id_srodka_trwalego | int(15) | TAK |
| statusinne | id_statusu_inne | int(11) | TAK |
| stawkavat | id_stawka_vat | int(11) | TAK |
| tabtym | id_tabtym | int(11) | TAK |
| text | id_tekst | int(11) | TAK |
| towarstatus | id_statusu_towaru | int(11) | TAK |
| towary | ID_towar | int(15) | TAK |
| umowypracownicy | id_umowy | int(15) | TAK |
| urlopy | id_urlopy | int(15) | TAK |
| waluty | id_waluty | int(11) | TAK |
| wyplaty | id_wyplaty | int(15) | TAK |
| wyplatydanem | id_wyplaty_dane_miesiaca | int(11) | TAK |
| wyplatydaney | id_wyplaty_dane_y | int(11) | TAK |
| zamowienia | id_zamowienia | int(15) | TAK |
| zamowieniahala | id_zamowienia_hala | int(11) | TAK |
| zamowieniestatus | id_statusu_zamowienia | int(11) | TAK |
| zlecenia | id_zlecenia | int(15) | TAK |

**Razem: 76 tabel**

