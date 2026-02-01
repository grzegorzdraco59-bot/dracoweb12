// Startup project w Solution musi być: ERP.UI.WPF (projekt z App.xaml).
// Uruchomienie: prawy klik na ERP.UI.WPF → "Set as Startup Project" lub: dotnet run --project ERP.UI.WPF

using System.IO;
using System.Linq;
using System.Windows;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Application.Services;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Repositories;
using ERP.Infrastructure.Services;
using ERP.UI.WPF.ViewModels;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;
using IUserContext = ERP.UI.WPF.Services.IUserContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.UI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private const string StartupLogPath = "logs/startup.log";
    private ServiceProvider? _serviceProvider;

    public App()
    {
        // Rejestracja najwcześniej – łapie wyjątki przed OnStartup (np. w InitializeComponent)
        AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

        WriteStartupLog("App() entered");
        try { MessageBox.Show("START: App() entered", "Diagnostyka"); }
        catch (Exception ex) { WriteStartupLog($"MessageBox w App(): {ex}"); }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        WriteStartupLog("OnStartup entered");
        try { MessageBox.Show("START: OnStartup entered", "Diagnostyka"); }
        catch (Exception ex) { WriteStartupLog($"MessageBox w OnStartup: {ex}"); }

        // ShutdownMode z App.xaml: OnMainWindowClose – aplikacja kończy się gdy zamknięte zostanie MainWindow

        // AppDomain.UnhandledException już zarejestrowany w App()
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        try
        {
            WriteStartupLog("ConfigureServices...");
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            WriteStartupLog("ServiceProvider OK");

            var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            loginViewModel.LoginSuccessful += OnLoginSuccessfulFromStartup;

            WriteStartupLog("Tworzenie LoginWindow...");
            var w = new LoginWindow(loginViewModel);
            // Zamykamy aplikację tylko gdy użytkownik zamknie LoginWindow BEZ logowania (MainWindow nadal = w)
            w.Closed += (_, _) => { if (MainWindow == w) Shutdown(); };
            MainWindow = w;
            w.Show();
            WriteStartupLog("LoginWindow.Show() OK");
        }
        catch (Exception ex)
        {
            LogExceptionToFile(StartupLogPath, ex);
            MessageBox.Show(ex.ToString(), "Błąd uruchamiania", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void WriteStartupLog(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(StartupLogPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n");
        }
        catch { /* ignoruj */ }
    }

    private static void LogExceptionToFile(string relativePath, Exception ex)
    {
        try
        {
            var dir = Path.GetDirectoryName(relativePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\r\n{ex}\r\n";
            if (ex.InnerException != null)
                text += $"Inner: {ex.InnerException}\r\n";
            File.AppendAllText(relativePath, text);
        }
        catch { /* ignoruj błędy zapisu logu */ }
    }

    private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        LogExceptionToFile(StartupLogPath, ex);
        MessageBox.Show(ex.ToString(), "Nieobsłużony wyjątek (AppDomain)", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogExceptionToFile(StartupLogPath, e.Exception);
        MessageBox.Show(e.Exception.ToString(), "Krytyczny błąd (Dispatcher)", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }

    /// <summary>LoginWindow (konstruktor bez parametrów) pobiera ViewModel z App.</summary>
    public LoginViewModel GetLoginViewModel() => _serviceProvider!.GetRequiredService<LoginViewModel>();

    /// <summary>MainWindow wymaga MainViewModel jako DataContext (np. po otwarciu z LoginWindow).</summary>
    public MainViewModel GetMainViewModel() => _serviceProvider!.GetRequiredService<MainViewModel>();

    private void OnLoginSuccessfulFromStartup(object? sender, (UserDto User, IEnumerable<CompanyDto> Companies) data)
    {
        var loggedInUser = data.User;
        var userCompanies = data.Companies;
        if (loggedInUser == null)
        {
            MessageBox.Show("Błąd: Brak danych użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }
        if (userCompanies == null || !userCompanies.Any())
        {
            MessageBox.Show(
                $"Użytkownik '{loggedInUser.FullName}' (ID: {loggedInUser.Id}) nie ma przypisanych żadnych firm.\n\n" +
                "Sprawdź czy w tabeli 'operatorfirma' są rekordy dla tego użytkownika.",
                "Brak firm",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
            return;
        }
        ShowCompanySelectionWindow(loggedInUser, userCompanies);
    }

    private void ShowLoginWindow()
    {
        try
        {
            var loginViewModel = _serviceProvider!.GetRequiredService<LoginViewModel>();
            UserDto? loggedInUser = null;
            IEnumerable<CompanyDto>? userCompanies = null;

            // Subskrypcja PRZED utworzeniem okna – przy sukcesie najpierw zapisujemy user/companies, potem LoginWindow zamyka się
            loginViewModel.LoginSuccessful += (sender, data) =>
            {
                loggedInUser = data.User;
                userCompanies = data.Companies;
            };

            var loginWindow = new LoginWindow(loginViewModel);
            
            if (loginWindow.ShowDialog() == true)
            {
                if (loggedInUser == null)
                {
                    MessageBox.Show("Błąd: Brak danych użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                if (userCompanies == null || !userCompanies.Any())
                {
                    MessageBox.Show(
                        $"Użytkownik '{loggedInUser.FullName}' (ID: {loggedInUser.Id}) nie ma przypisanych żadnych firm.\n\n" +
                        "Sprawdź czy w tabeli 'operatorfirma' są rekordy dla tego użytkownika.",
                        "Brak firm",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Shutdown();
                    return;
                }

                // Po zalogowaniu pokazujemy okno wyboru firmy
                ShowCompanySelectionWindow(loggedInUser, userCompanies);
            }
            else
            {
                // Użytkownik anulował logowanie - zamykamy aplikację
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            LogExceptionToFile(StartupLogPath, ex);
            MessageBox.Show(ex.ToString(), "Błąd otwierania okna logowania", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ShowCompanySelectionWindow(UserDto loggedInUser, IEnumerable<CompanyDto> userCompanies)
    {
        try
        {
            var userContext = _serviceProvider!.GetRequiredService<IUserContext>();
            var companySelectionViewModel = new CompanySelectionViewModel(
                userCompanies, 
                loggedInUser.Id, 
                loggedInUser.FullName,
                userContext);
            var companySelectionWindow = new CompanySelectionWindow(companySelectionViewModel);
            
            // Upewniamy się, że okno jest widoczne
            companySelectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            companySelectionWindow.ShowInTaskbar = true;
            companySelectionWindow.WindowState = WindowState.Normal;
            
            // Używamy ShowDialog() - to zablokuje wykonanie do czasu zamknięcia okna
            var dialogResult = companySelectionWindow.ShowDialog();
            
            if (dialogResult == true)
            {
                // Sprawdzamy, czy sesja została poprawnie ustawiona
                if (!userContext.IsLoggedIn)
                {
                    MessageBox.Show(
                        "Błąd: Sesja nie została poprawnie ustawiona. Spróbuj ponownie.",
                        "Błąd sesji",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                try
                {
                    // Po wyborze firmy pokazujemy główne okno
                    var mainWindow = new MainWindow();
                    
                    // Tworzymy MainViewModel przed ustawieniem DataContext
                    var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
                    mainWindow.DataContext = mainViewModel;
                    
                    MainWindow = mainWindow; // Ustawiamy jako główne okno aplikacji
                    
                    // Dodajemy obsługę zamknięcia okna, aby uniknąć powrotu do logowania
                    mainWindow.Closed += (s, e) =>
                    {
                        // Jeśli główne okno zostanie zamknięte, zamykamy aplikację
                        if (MainWindow == mainWindow)
                        {
                            Shutdown();
                        }
                    };
                    
                    // Upewniamy się, że okno jest widoczne przed pokazaniem
                    mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    mainWindow.ShowInTaskbar = true;
                    mainWindow.WindowState = WindowState.Normal;
                    
                    mainWindow.Show();
                    
                    // Sprawdzamy, czy okno jest nadal otwarte po pokazaniu
                    if (!mainWindow.IsLoaded)
                    {
                        MessageBox.Show(
                            "Błąd: Główne okno nie zostało poprawnie załadowane.",
                            "Błąd",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Błąd podczas otwierania głównego okna: {ex.Message}\n\n{ex.StackTrace}",
                        "Błąd",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                }
            }
            else
            {
                // Użytkownik anulował wybór firmy - zamykamy aplikację
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd podczas otwierania okna wyboru firmy: {ex.Message}\n\n{ex.StackTrace}", 
                "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Konfiguracja z appsettings.json (Copy to Output = PreserveNewest)
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Rejestracja UserContext - scoped service dla całej sesji aplikacji
        services.AddSingleton<IUserContext, UserContext>();
        services.AddSingleton<ERP.Application.Services.IUserContext>(sp => (ERP.Application.Services.IUserContext)sp.GetRequiredService<IUserContext>());

        // Rejestracja warstwy infrastruktury – DatabaseContext wymaga connection string z konfiguracji
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing in appsettings.json.");
        services.AddSingleton<DatabaseContext>(_ => new DatabaseContext(connectionString));
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        
        // Rejestracja repozytoriów
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserLoginRepository, UserLoginRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
        services.AddScoped<OperatorCompanyRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IOfferPositionRepository, OfferPositionRepository>();
        services.AddScoped<IOperatorTablePermissionRepository, OperatorTablePermissionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderMainRepository, OrderMainRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderPositionMainRepository, OrderPositionMainRepository>();
        services.AddScoped<ERP.Application.Repositories.IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ERP.Application.Repositories.IInvoicePositionRepository, InvoicePositionRepository>();
        services.AddScoped<ProductRepository>();
        services.AddScoped<WarehouseRepository>();
        
        // Rejestracja serwisów aplikacyjnych
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IOperatorPermissionService, OperatorPermissionService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IOrderFromOfferConversionService, OrderFromOfferConversionService>();
        services.AddScoped<IInvoiceTotalsService, InvoiceTotalsService>();
        services.AddScoped<IOfferTotalsService, OfferTotalsService>();
        services.AddScoped<IOfferToFpfConversionService, OfferToFpfConversionService>();
        services.AddScoped<IOrderMainService, OrderMainService>();

        // Automatyczna rejestracja walidatorów z ERP.Application.Validation (AddScoped, same typy)
        var applicationAssembly = typeof(ERP.Application.Services.CustomerService).Assembly;
        foreach (var type in applicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "ERP.Application.Validation"))
        {
            services.AddScoped(type);
        }
        
        // Rejestracja ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<OffersViewModel>();
        services.AddTransient<InvoicesViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<OrdersMainViewModel>();
        services.AddTransient<OrderPositionsViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<OperatorCompanyListViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
