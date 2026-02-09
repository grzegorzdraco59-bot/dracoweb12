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
builder.Services.AddSingleton<ERP.Infrastructure.Services.IConnectionStringProvider, ERP.Infrastructure.Services.ConnectionStringProvider>();
builder.Services.AddSingleton<DatabaseContext>();

// Dependency Injection - Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserLoginRepository, UserLoginRepository>();
builder.Services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ERP.Application.Repositories.ICompanyQueryRepository, CompanyRepository>();
builder.Services.AddScoped<ERP.Application.Repositories.IKontrahenciQueryRepository, KontrahenciQueryRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IOfferPositionRepository, OfferPositionRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderMainRepository, OrderMainRepository>();
builder.Services.AddScoped<IOrderPositionMainRepository, OrderPositionMainRepository>();
builder.Services.AddScoped<IInvoiceRepository, ERP.Infrastructure.Repositories.InvoiceRepository>();
builder.Services.AddScoped<IInvoicePositionRepository, ERP.Infrastructure.Repositories.InvoicePositionRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<IOperatorTablePermissionRepository, OperatorTablePermissionRepository>();

// Dependency Injection - Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOperatorPermissionService, OperatorPermissionService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<IOrderFromOfferConversionService, ERP.Infrastructure.Services.OrderFromOfferConversionService>();
builder.Services.AddScoped<IOrderMainService, OrderMainService>();

// UserContext - oparty o IHttpContextAccessor i Claims
builder.Services.AddScoped<ERP.UI.Web.Services.IUserContext, ERP.UI.Web.Services.UserContext>();
builder.Services.AddScoped<ERP.Application.Services.IUserContext>(sp => (ERP.Application.Services.IUserContext)sp.GetRequiredService<ERP.UI.Web.Services.IUserContext>());

// Unit of Work dla transakcji
builder.Services.AddScoped<ERP.Infrastructure.Services.IUnitOfWork, ERP.Infrastructure.Services.UnitOfWork>();
builder.Services.AddScoped<ERP.Infrastructure.Services.IIdGenerator, ERP.Infrastructure.Services.IdGeneratorService>();
builder.Services.AddScoped<IDocumentNumberService, ERP.Infrastructure.Services.DocumentNumberService>();
builder.Services.AddScoped<IInvoiceTotalsService, ERP.Infrastructure.Services.InvoiceTotalsService>();

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
    // Kontrahenci
    options.AddPolicy("Kontrahenci:Read", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("kontrahenci", "SELECT"));
    });
    options.AddPolicy("Kontrahenci:Write", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("kontrahenci", "INSERT"));
        policy.Requirements.Add(new TablePermissionRequirement("kontrahenci", "UPDATE"));
    });
    options.AddPolicy("Kontrahenci:Delete", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("kontrahenci", "DELETE"));
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

    // Invoices (Faktury)
    options.AddPolicy("Invoices:Read", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("faktury", "SELECT"));
    });
    options.AddPolicy("Invoices:Write", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("faktury", "INSERT"));
        policy.Requirements.Add(new TablePermissionRequirement("faktury", "UPDATE"));
    });
    options.AddPolicy("Invoices:Delete", policy =>
    {
        policy.Requirements.Add(new CompanyAccessRequirement());
        policy.Requirements.Add(new TablePermissionRequirement("faktury", "DELETE"));
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
