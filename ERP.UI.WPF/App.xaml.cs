using System.Windows;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.Application.Services;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Data;
using ERP.Infrastructure.Repositories;
using ERP.UI.WPF.ViewModels;
using ERP.UI.WPF.Views;
using ERP.UI.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.UI.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Globalna obsługa nieobsłużonych wyjątków
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        try
        {
            // Konfiguracja Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Najpierw pokazujemy okno logowania
            ShowLoginWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd podczas uruchamiania aplikacji:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Błąd uruchamiania",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Nieobsłużony wyjątek:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "Krytyczny błąd",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }

    private void ShowLoginWindow()
    {
        try
        {
            var loginViewModel = _serviceProvider!.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow(loginViewModel);
        
            UserDto? loggedInUser = null;
            IEnumerable<CompanyDto>? userCompanies = null;
            
            loginViewModel.LoginSuccessful += (sender, data) =>
            {
                loggedInUser = data.User;
                userCompanies = data.Companies;
                // DialogResult i Close() są obsługiwane w LoginWindow.OnLoginSuccessful
            };
            
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
            MessageBox.Show(
                $"Błąd podczas otwierania okna logowania:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
        // Rejestracja UserContext - scoped service dla całej sesji aplikacji
        services.AddSingleton<IUserContext, UserContext>();
        
        // Rejestracja warstwy infrastruktury
        services.AddSingleton<DatabaseContext>();
        
        // Rejestracja repozytoriów
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserLoginRepository, UserLoginRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IUserCompanyRepository, UserCompanyRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IOfferPositionRepository, OfferPositionRepository>();
        services.AddScoped<IOperatorTablePermissionRepository, OperatorTablePermissionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderMainRepository, OrderMainRepository>();
        services.AddScoped<ERP.Application.Repositories.IOrderPositionMainRepository, OrderPositionMainRepository>();
        services.AddScoped<ProductRepository>();
        services.AddScoped<WarehouseRepository>();
        
        // Rejestracja serwisów aplikacyjnych
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IOperatorPermissionService, OperatorPermissionService>();
        services.AddScoped<IOrderService, OrderService>();
        
        // Rejestracja ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<OffersViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<OrdersViewModel>();
        services.AddTransient<OrdersMainViewModel>();
        services.AddTransient<OrderPositionsViewModel>();
        services.AddTransient<AdminViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
