using ERP.Application.Services;
using ERP.Application.Repositories;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Repositories;
using Microsoft.AspNetCore.Session;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ERP.UI.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Dodaj User Secrets dla development (opcjonalne)
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpContext Accessor - potrzebny do repozytoriów
builder.Services.AddHttpContextAccessor();

// Konfiguracja autentykacji Cookie (Model mentalny: Autoryzacja = Identity / Claims)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// Konfiguracja sesji
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Dependency Injection - Database Context
builder.Services.AddSingleton<DatabaseContext>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' nie został znaleziony. " +
            "Upewnij się, że connection string jest skonfigurowany w appsettings.json lub User Secrets.");
    }
    return new DatabaseContext(connectionString);
});

// Dependency Injection - Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserLoginRepository, UserLoginRepository>();
builder.Services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IOfferPositionRepository, OfferPositionRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderMainRepository, OrderMainRepository>();
builder.Services.AddScoped<IOrderPositionMainRepository, OrderPositionMainRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<IOperatorTablePermissionRepository, OperatorTablePermissionRepository>();

// Dependency Injection - Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOperatorPermissionService, OperatorPermissionService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// UserContext - oparty o IHttpContextAccessor i Claims
builder.Services.AddScoped<ERP.UI.Web.Services.IUserContext, ERP.UI.Web.Services.UserContext>();

// Unit of Work dla transakcji
builder.Services.AddScoped<ERP.Infrastructure.Services.IUnitOfWork, ERP.Infrastructure.Services.UnitOfWork>();

// Validators
builder.Services.AddScoped<ERP.Application.Validation.CustomerValidator>();
builder.Services.AddScoped<ERP.Application.Validation.CustomerDtoValidator>();

// Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, CompanyAccessAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TablePermissionAuthorizationHandler>();

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Policy: Użytkownik musi mieć dostęp do wybranej firmy
    options.AddPolicy("CompanyAccess", policy =>
        policy.Requirements.Add(new CompanyAccessRequirement()));

    // Policy: Użytkownik musi mieć rolę (jakąkolwiek)
    options.AddPolicy("HasRole", policy =>
        policy.Requirements.Add(new RoleRequirement()));

    // Policy: Użytkownik musi mieć dostęp do firmy I rolę
    options.AddPolicy("CompanyAccessAndRole", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new RoleRequirement());
    });

    // Policies dla uprawnień do tabel
    // Customers (Odbiorcy)
    options.AddPolicy("Customers:Read", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("Odbiorcy", "SELECT"));
    });
    options.AddPolicy("Customers:Write", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("Odbiorcy", "INSERT"));
        policy.Requirements.Add(new TablePermissionRequirement("Odbiorcy", "UPDATE"));
    });
    options.AddPolicy("Customers:Delete", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("Odbiorcy", "DELETE"));
    });

    // Suppliers (Dostawcy)
    options.AddPolicy("Suppliers:Read", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("dostawcy", "SELECT"));
    });
    options.AddPolicy("Suppliers:Write", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("dostawcy", "INSERT"));
        policy.Requirements.Add(new TablePermissionRequirement("dostawcy", "UPDATE"));
    });
    options.AddPolicy("Suppliers:Delete", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("dostawcy", "DELETE"));
    });

    // Products (Towary)
    options.AddPolicy("Products:Read", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("towary", "SELECT"));
    });
    options.AddPolicy("Products:Write", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("towary", "INSERT"));
        policy.Requirements.Add(new TablePermissionRequirement("towary", "UPDATE"));
    });
    options.AddPolicy("Products:Delete", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("towary", "DELETE"));
    });

    // Offers (Oferty)
    options.AddPolicy("Offers:Read", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("aoferty", "SELECT"));
    });
    options.AddPolicy("Offers:Write", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("aoferty", "INSERT"));
        policy.Requirements.Add(new TablePermissionRequirement("aoferty", "UPDATE"));
    });
    options.AddPolicy("Offers:Delete", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("aoferty", "DELETE"));
    });

    // Orders (Zamówienia)
    options.AddPolicy("Orders:Read", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("zamowienia", "SELECT"));
    });
    options.AddPolicy("Orders:Write", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("zamowienia", "INSERT"));
        policy.Requirements.Add(new TablePermissionRequirement("zamowienia", "UPDATE"));
    });
    options.AddPolicy("Orders:Delete", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("zamowienia", "DELETE"));
    });

    // Admin - wymaga roli administratora (można rozszerzyć o konkretną rolę)
    options.AddPolicy("Admin", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new RoleRequirement());
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// UseHttpsRedirection wyłączone w DEV – brak nasłuchu HTTPS w launchSettings; włączone w osobnej fazie
// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Włącz sesję (Model mentalny: Firma = stan aplikacji w sesji)
app.UseSession();

// Autentykacja i autoryzacja (Model mentalny: Autoryzacja = Identity / Claims)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run("http://localhost:5049");
