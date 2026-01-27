# Konfiguracja Bezpieczeństwa - dracoWEB5

## Zmiany wprowadzone

### 1. Hashowanie haseł - BCrypt
- ✅ Zastąpiono SHA256 na BCrypt.Net-Next
- ✅ Weryfikacja obsługuje zarówno stare hasła (SHA256) jak i nowe (BCrypt)
- ✅ Nowe hasła są automatycznie hashowane używając BCrypt

### 2. Usunięte hardcoded hasła
- ✅ Usunięto hardcoded connection strings z kodu źródłowego
- ✅ Connection strings są teraz w appsettings.json i User Secrets

## Konfiguracja

### Development (User Secrets)

Dla środowiska development użyj User Secrets:

```bash
cd ERP.UI.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=twoje_haslo;SslMode=None;"
```

### Production (appsettings.json)

W środowisku produkcyjnym connection string powinien być w `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=twoj_serwer;Port=3306;Database=twoja_baza;User Id=twoj_user;Password=twoje_haslo;SslMode=Required;"
  }
}
```

**WAŻNE:** Plik `appsettings.Production.json` NIE powinien być commitowany do repozytorium!

### appsettings.json

Plik `appsettings.json` zawiera placeholder:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=locbd;User Id=root;Password=PLACEHOLDER_PASSWORD;SslMode=None;"
  }
}
```

Zamień `PLACEHOLDER_PASSWORD` na rzeczywiste hasło lub użyj User Secrets.

## Migracja haseł

System obsługuje zarówno stare hasła (SHA256) jak i nowe (BCrypt). 
Przy pierwszym logowaniu z hasłem SHA256, zalecane jest:
1. Zalogować się używając starego hasła
2. Zmienić hasło (zostanie zapisane jako BCrypt)
3. Następne logowania będą używać BCrypt

## Bezpieczeństwo

- ✅ Hasła są hashowane używając BCrypt (work factor: 12)
- ✅ Connection strings nie są w kodzie źródłowym
- ✅ User Secrets dla development
- ✅ appsettings.Production.json dla production (nie w repo)
