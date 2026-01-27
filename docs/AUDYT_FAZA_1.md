# Audyt Faza 1 – zamknięcie

**Projekt:** dracoweb12 (ERP.UI.Web)  
**Data zamknięcia fazy:** 2026-01-27

## Ustalenia

- **Problem:** Włączony był `UseHttpsRedirection` przy braku nasłuchu HTTPS w konfiguracji (launchSettings.json zawierał wyłącznie `http://localhost:5049`). Przekierowanie na HTTPS prowadziło do adresu, na którym aplikacja nie nasłuchiwała, co skutkowało błędami połączenia (m.in. ERR_CONNECTION_REFUSED).
- **Rozwiązanie:** Wyłączenie `UseHttpsRedirection` w środowisku DEV. Po wyłączeniu redirectu aplikacja startuje i jest dostępna na `http://localhost:5049`.

## Stan działania po Fazie 1

- Aplikacja startuje.
- Logowanie działa.
- Wybór firmy działa.
- Otwarcie strony głównej po zalogowaniu i wyborze firmy działa.

## Punkt odniesienia

Ten stan jest zatwierdzonym punktem odniesienia do dalszych prac. Wdrożenie HTTPS planowane jest jako osobna faza.
