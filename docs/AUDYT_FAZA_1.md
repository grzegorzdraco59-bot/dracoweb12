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

DEV działa na HTTP; HTTPS będzie konfigurowany w osobnej fazie.

## Checkpoint: audit-faza1_ok_faza2k1_ok

Działa: start HTTP, login, wybór firmy, Main, anti-forgery.

## Checkpoint: audit-faza1_ok_faza2k2_ok

Działa: start HTTP, login, wybór firmy, anti-forgery, returnUrl.

## Checkpoint: audit-faza1_ok_faza2k4_ok

Działa: start HTTP, login, wybór firmy, anti-forgery, returnUrl, uproszczony UserId.

## Notatka developerska: model ról

Model ról w aplikacji = **RoleId** (int, claim `"RoleId"`), a nie `ClaimTypes.Role`. Policies i handlery autoryzacji opierają się na RoleId. Właściwość `IUserContext.Roles` (ClaimTypes.Role) nie jest używana.

## Checkpoint: audit-faza1_ok_faza2k5_ok

Działa: start HTTP, login, wybór firmy, anti-forgery, returnUrl, uproszczony UserId, role model = RoleId.

## Checkpoint: faza3a_wpf_login_okno_glowne_ok

WPF: po loginie otwiera się okno główne.

## Checkpoint: faza3a_wpf_menu_ok_di_sql_ok

WPF: działają widoki 1,8,9,10,12,13; DI WPF kompletne; repo odcięte od IHttpContextAccessor; poprawione ORDER BY w SQL.

## Checkpoint: faza3a_krok4_userlogin_uow_ok

WPF: UserLoginEditViewModel SaveAsync (DELETE+INSERT) w transakcji.
