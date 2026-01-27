# dracoWEB

Kopia projektu **MariaDbTest** zapisana jako **dracoWEB**.

Zawiera m.in.:
- **ERP.UI.Web** – aplikacja webowa DracoERP (ASP.NET Core MVC)
- ERP.Application, ERP.Domain, ERP.Infrastructure
- ERP.Migrations, ERP.Reports, ERP.Shared, ERP.Tests
- ERP.UI.WPF, RunMigration, TestConnection

## Uruchomienie

```powershell
cd ERP.UI.Web
dotnet watch run
```

Aplikacja: **http://localhost:5049**

## Rozwiązanie

- `dracoWEB.sln` – główne rozwiązanie (zalecane)
- `ERP.sln` – kopia oryginalnego rozwiązania
