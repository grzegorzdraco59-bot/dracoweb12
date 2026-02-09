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
using IConnectionStringProvider = ERP.Infrastructure.Services.IConnectionStringProvider;
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
    private bool _mainOpened; // Guard: Main otwierany tylko raz (ostatnia linia obrony przy double event)

    public App()
    {
        // Rejestracja najwcześniej – łapie wyjątki przed OnStartup (np. w InitializeComponent)
        AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
        WriteStartupLog("App() entered");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory("logs");
        WriteStartupLog("OnStartup entered");
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
            DatabaseContext.OnFirstConnectionDiagnostic = msg =>
                Dispatcher.Invoke(() => MessageBox.Show(msg, "Diagnostyka DB (realne połączenie)", MessageBoxButton.OK, MessageBoxImage.Information));

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
            MessageBox.Show(ex.ToString(), "STARTUP ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
            Directory.CreateDirectory("logs");
            var dir = Path.GetDirectoryName(relativePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\r\n{ex.ToString()}\r\n";
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
        MessageBox.Show(ex.ToString(), "STARTUP ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogExceptionToFile(StartupLogPath, e.Exception);
        MessageBox.Show(e.Exception.ToString(), "STARTUP ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }

    /// <summary>LoginWindow (konstruktor bez parametrów) pobiera ViewModel z App.</summary>
    public LoginViewModel GetLoginViewModel() => _serviceProvider!.GetRequiredService<LoginViewModel>();

    /// <summary>MainWindow wymaga MainViewModel jako DataContext (np. po otwarciu z LoginWindow).</summary>
    public MainViewModel GetMainViewModel() => _serviceProvider!.GetRequiredService<MainViewModel>();

    /// <summary>UserContext do guardów (np. MainWindow).</summary>
    public IUserContext GetUserContext() => _serviceProvider!.GetRequiredService<IUserContext>();

    /// <summary>Ogólny dostęp do serwisów z kontenera DI.</summary>
    public T GetService<T>() where T : notnull => _serviceProvider!.GetRequiredService<T>();

    private async void OnLoginSuccessfulFromStartup(object? sender, (UserDto User, IEnumerable<CompanyDto> Companies) data)
    {
        var loggedInUser = data.User;
        var userCompanies = (data.Companies ?? Enumerable.Empty<CompanyDto>()).ToList();
        var loginWindow = Current.MainWindow as LoginWindow;

        if (loggedInUser == null)
        {
            MessageBox.Show("Błąd: Brak danych użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }
        var authService = _serviceProvider!.GetRequiredService<IAuthenticationService>();
        if (!await authService.HasCompaniesForUserAsync(loggedInUser.Id))
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

        var userContext = _serviceProvider!.GetRequiredService<IUserContext>();

        if (userCompanies.Count == 1)
        {
            // 1 firma: ustaw sesję i od razu otwórz Main (bez wyboru firmy)
            var company = userCompanies[0];
            userContext.SetSession(loggedInUser.Id, company.Id, company.RoleId, loggedInUser.FullName ?? "");
            OpenMainWindowAndCloseLogin(loginWindow);
        }
        else
        {
            // >1 firm: otwórz TYLKO SelectCompany, NIE otwieraj Main
            ShowCompanySelectionWindow(loggedInUser, userCompanies, loginWindow);
        }
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

                // Walidacja: SELECT COUNT(*) FROM operatorfirma WHERE id_operatora = @UserId. Komunikat tylko gdy COUNT = 0.
                var authService = _serviceProvider!.GetRequiredService<IAuthenticationService>();
                if (!authService.HasCompaniesForUserAsync(loggedInUser.Id).GetAwaiter().GetResult())
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

                // Po zalogowaniu: 1 firma → Main, >1 → SelectCompany (loginWindow już zamknięty po ShowDialog)
                var companiesList = (userCompanies ?? Enumerable.Empty<CompanyDto>()).ToList();
                var uctx = _serviceProvider!.GetRequiredService<IUserContext>();
                if (companiesList.Count == 1)
                {
                    var c = companiesList[0];
                    uctx.SetSession(loggedInUser.Id, c.Id, c.RoleId, loggedInUser.FullName ?? "");
                    OpenMainWindowAndCloseLogin(null);
                }
                else
                {
                    ShowCompanySelectionWindow(loggedInUser, companiesList, null);
                }
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

    /// <summary>Otwiera Main (jedna instancja) i zamyka LoginWindow (lub placeholder).</summary>
    private void OpenMainWindowAndCloseLogin(Window? loginWindow)
    {
        if (_mainOpened)
            return;
        _mainOpened = true;
        try
        {
            void DoOpenMain()
            {
                var userContext = _serviceProvider!.GetRequiredService<IUserContext>();
                if (!userContext.IsLoggedIn || !userContext.CompanyId.HasValue)
                {
                    MessageBox.Show("Błąd: Brak wybranej firmy. Nie można otworzyć głównego okna.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                var mainWindow = new MainWindow();
                var mainViewModel = _serviceProvider!.GetRequiredService<MainViewModel>();
                mainWindow.DataContext = mainViewModel;
                MainWindow = mainWindow;

                mainWindow.Closed += (_, _) =>
                {
                    if (MainWindow == mainWindow) Shutdown();
                };

                mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                mainWindow.ShowInTaskbar = true;
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Show();

                // Bezpieczne zamknięcie: tylko gdy nie null (loginWindow może być null gdy wywołanie z AfterCompanySelection)
                if (loginWindow != null)
                {
                    try { loginWindow.Close(); } catch { /* okno mogło być już zamknięte */ }
                }
            }

            var dispatcher = Current?.Dispatcher ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
            if (dispatcher.CheckAccess())
                DoOpenMain();
            else
                dispatcher.Invoke(DoOpenMain);
        }
        catch (Exception ex)
        {
            LogExceptionToFile(StartupLogPath, ex);
            MessageBox.Show($"Błąd podczas otwierania głównego okna: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ShowCompanySelectionWindow(UserDto loggedInUser, IEnumerable<CompanyDto> userCompanies, Window? loginWindow)
    {
        try
        {
            var userContext = _serviceProvider!.GetRequiredService<IUserContext>();
            var companySelectionViewModel = new CompanySelectionViewModel(
                userCompanies,
                loggedInUser.Id,
                loggedInUser.FullName ?? "",
                userContext);
            var companySelectionWindow = new CompanySelectionWindow(companySelectionViewModel);

            companySelectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            companySelectionWindow.ShowInTaskbar = true;
            companySelectionWindow.WindowState = WindowState.Normal;

            // Placeholder: nigdy nie ustawiamy CompanySelection jako MainWindow – gdy się zamknie, wywołałoby Shutdown.
            // Używamy niewidocznego placeholder, żeby po zamknięciu Login nie wywołać Shutdown.
            var placeholder = new Window
            {
                ShowInTaskbar = false,
                Width = 1,
                Height = 1,
                WindowState = WindowState.Minimized,
                Visibility = Visibility.Collapsed
            };
            MainWindow = placeholder;
            if (loginWindow != null) { try { loginWindow.Close(); } catch { } }

            var dialogResult = companySelectionWindow.ShowDialog();

            if (dialogResult == true)
            {
                if (!userContext.IsLoggedIn)
                {
                    MessageBox.Show("Błąd: Sesja nie została poprawnie ustawiona.", "Błąd sesji", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                OpenMainWindowAndCloseLogin(placeholder); // Zamknięcie placeholder przy otwarciu Main
            }
            else
            {
                Shutdown();
            }
        }
        catch (Exception ex)
        {
            LogExceptionToFile(StartupLogPath, ex);
            MessageBox.Show($"Błąd podczas otwierania okna wyboru firmy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Konfiguracja z appsettings.json (Copy to Output = PreserveNewest)
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Rejestracja UserContext - scoped service dla całej sesji aplikacji
        services.AddSingleton<IUserContext, UserContext>();
        services.AddSingleton<ERP.Application.Services.IUserContext>(sp => (ERP.Application.Services.IUserContext)sp.GetRequiredService<IUserContext>());
        services.AddTransient<ERP.UI.WPF.Services.ITowarPicker, ERP.UI.WPF.Services.TowarPicker>();

        // ConnectionStringProvider – wybór bazy: LOCBD | DRACO_OFFICE_WIFI | DRACO_REMOTE
        services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
        services.AddSingleton<DatabaseContext>(sp => new DatabaseContext(sp.GetRequiredService<IConnectionStringProvider>()));
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        services.AddScoped<IIdGenerator, IdGeneratorService>();
        
        // Rejestracja repozytoriów
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserLoginRepository, UserLoginRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ERP.Application.Repositories.ICompanyQueryRepository, CompanyRepository>();
        services.AddScoped<ERP.Application.Repositories.IKontrahenciQueryRepository, KontrahenciQueryRepository>();
        services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
        services.AddScoped<OperatorCompanyRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IOfferPositionRepository, OfferPositionRepository>();
        services.AddScoped<IOperatorTablePermissionRepository, OperatorTablePermissionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderMainRepository, OrderMainRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderRowRepository, OrderRowRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderPositionRepository, OrderPositionRowRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderPositionMainRepository, OrderPositionMainRepository>();
        services.AddScoped<ERP.Application.Repositories.IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ERP.Application.Repositories.IInvoicePositionRepository, InvoicePositionRepository>();
        services.AddScoped<ProductRepository>();
        services.AddScoped<WarehouseRepository>();
        
        // Rejestracja serwisów aplikacyjnych
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IOperatorPermissionService, OperatorPermissionService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IOrderFromOfferConversionService, OrderFromOfferConversionService>();
        services.AddScoped<IInvoiceTotalsService, InvoiceTotalsService>();
        services.AddScoped<IOfferTotalsService, OfferTotalsService>();
        services.AddScoped<IOfferToFpfConversionService, OfferToFpfConversionService>();
        services.AddScoped<IOfferToZlecenieConversionService, OfferToZlecenieConversionService>();
        services.AddScoped<IInvoiceCopyService, InvoiceCopyService>();
        services.AddScoped<IOfferPdfService, OfferPdfService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IOrderMainService, OrderMainService>();
        services.AddScoped<IKontrahenciCommandRepository, KontrahenciCommandRepository>();

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
        services.AddTransient<KontrahenciViewModel>();
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
